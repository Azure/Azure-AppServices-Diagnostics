using System;
using System.Collections.Generic;
using System.IO;
using Diagnostics.DataProviders.Utility;

namespace Diagnostics.DataProviders
{ /// <summary>
  /// The class to deserialize the binary payload in response.
  /// </summary>
    public sealed class MetricQueryResponseDeserializer
    {
        /// <summary>
        /// The HTTP header for the number of data points in response.
        /// </summary>
        public const string NumDataPointsHeader = "__NumDataPoints__";

        /// <summary>
        /// The current stable version.
        /// </summary>
        public const byte CurrentVersion = 3;

        /// <summary>
        /// The next version to be supported in the future, if different from <see cref="CurrentVersion"/>.
        /// </summary>
        /// <remarks>
        /// The changes in V3 are to correct <see cref="NumBitsToEncodeNumLeadingZeros"/> from 5 to 6.
        /// The changes in V2 include: 1) use algorithm described in Gorilla paper to bit-wise encode metric double values, 2) remove the notion of sparse data, and 3) let server handle scaling factor.
        /// The V1 doc: https://microsoft.sharepoint.com/teams/WAG/EngSys/Monitor/Shared%20Documents/Metrics%20Collection/OBO.docx?d=w4e80d41fe9a544049320b8e3023e5f6d
        /// </remarks>
        public const byte NextVersion = 3;

        /// <summary>
        /// The number of bits used to encode the number of meaningful bits.
        /// </summary>
        public const int NumBitsToEncodeNumMeaningfulBits = 6;

        /// <summary>
        /// The number of bits used to encode the number of leading zeros.
        /// </summary>
        /// <remarks>
        /// This number was taken from Gorilla paper below. I had doubt in reading the paper but dismissed it somehow.
        /// For a 64-bit long integer, the number of leading zeros could be more than 32, needing 6 bits to encode.
        /// </remarks>
        private const int NumBitsToEncodeNumLeadingZeros = 5;

        /// <summary>
        /// The number of bits used to encode the number of leading zeros.
        /// </summary>
        private const int NumBitsToEncodeNumLeadingZerosCorrected = 6;

        /// <summary>
        /// The sampling types with value type of long (instead of double).
        /// </summary>
        private static readonly SamplingType[] SamplingTypesWithValueTypeOfLong = new[]
            {
                SamplingType.Count,
                SamplingType.Sum,
                SamplingType.Min,
                SamplingType.Max,
                SamplingType.Percentile50th,
                SamplingType.Percentile90th,
                SamplingType.Percentile95th,
                SamplingType.Percentile99th,
                SamplingType.Percentile999th
            };

        /// <summary>
        /// The default date time.
        /// </summary>
        private static readonly DateTime DefaultDateTime = default(DateTime);

        /// <summary>
        /// Gets the number of bits to encode the number of leading zeros based on the serialization version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>The number of bits to encode the number of leading zeros.</returns>
        public static int GetNumBitsToEncodeNumLeadingZeros(int version)
        {
            return version >= 3 ? NumBitsToEncodeNumLeadingZerosCorrected : NumBitsToEncodeNumLeadingZeros;
        }

        /// <summary>
        /// Deserializes the specified stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize.</param>
        /// <param name="definitions">The time series definitions.</param>
        /// <param name="numSamplingTypesRequested">The number of requested sampling types. OBO needs to specify this since it doesn't know the <paramref name="definitions"/>.</param>
        /// <returns>
        /// Item1 of the tuple is the list of metric names, and Item2 is the time series values.
        /// When <paramref name="definitions" /> is null (OBO case), each time series object contains only the error code and the metric values queryable via sampling type indexes.
        /// </returns>
        public static Tuple<string[], TimeSeries<MetricIdentifier, double?>[]> Deserialize(
            Stream stream,
            IReadOnlyList<TimeSeriesDefinition<MetricIdentifier>> definitions,
            int numSamplingTypesRequested = -1)
        {
            if (definitions == null && numSamplingTypesRequested <= 0)
            {
                throw new ArgumentException($"{numSamplingTypesRequested} must be > 0 when {nameof(definitions)} is null.");
            }

            using (var reader = new BinaryReader(stream))
            {
                var version = reader.ReadByte();

                if (version > NextVersion)
                {
                    throw new MetricsClientException($"The server didn't respond with the right version of serialization. CurrentVersion : {CurrentVersion}, NextVersion : {NextVersion}, Responded: {version}.");
                }

                // Get the number of metric names required for OBO scenario since OBO service doesn't know the metrics to query,
                // and it is MdmRP that fills them in.
                var numMetricNames = reader.ReadInt16();

                string[] metricNames;
                if (numMetricNames == 0)
                {
                    metricNames = null;
                }
                else
                {
                    metricNames = new string[(int)numMetricNames];
                    for (int i = 0; i < numMetricNames; ++i)
                    {
                        metricNames[i] = reader.ReadString();
                    }
                }

                var numSeries = SerializationUtils.ReadUInt32FromBase128(reader);

                TimeSeries<MetricIdentifier, double?>[] seriesArray = null;

                seriesArray = new TimeSeries<MetricIdentifier, double?>[(int)numSeries];

                for (int i = 0; i < numSeries; i++)
                {
                    var deserialized = DeserializeOneSeries(version, reader, definitions?[i], numSamplingTypesRequested);
                    if (definitions == null)
                    {
                        seriesArray[i] = new TimeSeries<MetricIdentifier, double?>(DefaultDateTime, DefaultDateTime, 1, null, deserialized.Values, deserialized.ErrorCode);
                    }
                    else
                    {
                        var definition = definitions[i];
                        if (deserialized.Values != null)
                        {
                            var startTime = definition.StartTimeUtc.AddMinutes(deserialized.DeltaOfStartTimeInMinutes);

                            var resolutionWindow = definition.SeriesResolutionInMinutes + deserialized.DeltaOfSeriesResolutionInMinutes;

                            var endTimeUtc = startTime + TimeSpan.FromMinutes(resolutionWindow * (deserialized.Values[0].Count - 1));

                            seriesArray[i] = new TimeSeries<MetricIdentifier, double?>(startTime, endTimeUtc, resolutionWindow, definition, deserialized.Values, deserialized.ErrorCode);
                        }
                        else
                        {
                            seriesArray[i] = new TimeSeries<MetricIdentifier, double?>(definition.StartTimeUtc, definition.EndTimeUtc, definition.SeriesResolutionInMinutes, definition, deserialized.Values, deserialized.ErrorCode);
                        }
                    }
                }

                return Tuple.Create(metricNames, seriesArray);
            }
        }

        /// <summary>
        /// Determines whether the metric value is of type long.
        /// </summary>
        /// <param name="samplingType">Type of the sampling.</param>
        /// <param name="seriesResolutionInMinutes">The series resolution in minutes.</param>
        /// <param name="aggregationType">Type of the aggregation.</param>
        /// <returns>
        /// True if the metric value is of type long; false otherwise.
        /// </returns>
        public static bool IsMetricValueTypeLong(SamplingType samplingType, int seriesResolutionInMinutes, AggregationType aggregationType)
        {
            // AggregationType.None is the default, which is to compute the average.
            if (aggregationType == AggregationType.None && seriesResolutionInMinutes > 1)
            {
                return false;
            }

            for (int i = 0; i < SamplingTypesWithValueTypeOfLong.Length; ++i)
            {
                if (SamplingTypesWithValueTypeOfLong[i].Equals(samplingType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Deserializes one series.
        /// </summary>
        /// <param name="version">The serialization version.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="numSamplingTypesRequested">The number of sampling types.</param>
        /// <returns>An <see cref="DeserializedRawData"/> object.</returns>
        private static DeserializedRawData DeserializeOneSeries(byte version, BinaryReader reader, TimeSeriesDefinition<MetricIdentifier> definition, int numSamplingTypesRequested)
        {
            // Read the number of data points.
            var totalNumberOfDatapoints = SerializationUtils.ReadInt32FromBase128(reader);
            if (totalNumberOfDatapoints < 0)
            {
                // Failed to query the series. We reuse the same byte(s) for totalNumberOfDatapoints for error codes.
                return new DeserializedRawData(0, 0, null, (TimeSeriesErrorCode)totalNumberOfDatapoints);
            }

            // Read the number of missing data points or those with null values so that we can get the total size of data points next.
            // in V2 and beyond, totalNumberMissingDatapoints is always to set to 0 since each missing data point in a very sparse series just uses a single bit for padding,
            // and scaling will be done on the server side.
            uint totalNumberMissingDatapoints = 0;
            uint scalingFactor = 1;
            if (version == 1)
            {
                totalNumberMissingDatapoints = SerializationUtils.ReadUInt32FromBase128(reader);

                // We want to apply the scaling factor on the client so that server can return metric values of type long with variable length encoding.
                scalingFactor = SerializationUtils.ReadUInt32FromBase128(reader);
            }

            var numberDatapointsToDeserialize = totalNumberOfDatapoints - (int)totalNumberMissingDatapoints;

            var numSamplingTypes = definition?.SamplingTypes.Length ?? numSamplingTypesRequested;

            var values = new List<List<double?>>(numSamplingTypes);
            for (int index = 0; index < numSamplingTypes; index++)
            {
                values.Add(new List<double?>(totalNumberOfDatapoints));
            }

            // We use deltas/differences as compared with the prior data point.
            // Although deltas/differences don't help values of type double, it will when we encode double type in the future.
            var priorValidValues = new double[numSamplingTypes];
            var sampleTyesWithMetricValueTypeLong = new bool[numSamplingTypes];

            // Used for implementing the Gorilla algorithm
            sbyte[] currentBlockLeadingZeros = new sbyte[numSamplingTypes];
            sbyte[] currentBlockTrailingZeros = new sbyte[numSamplingTypes];

            for (int index = 0; index < numSamplingTypes; index++)
            {
                priorValidValues[index] = 0;
                currentBlockLeadingZeros[index] = -1;
                currentBlockTrailingZeros[index] = -1;

                if (definition == null || IsMetricValueTypeLong(definition.SamplingTypes[index], definition.SeriesResolutionInMinutes, definition.AggregationType))
                {
                    sampleTyesWithMetricValueTypeLong[index] = true;
                }
            }

            var sparseData = totalNumberMissingDatapoints > 0;
            var bitReader = new BitBinaryReader(reader);
            for (int d = 0; d < numberDatapointsToDeserialize; ++d)
            {
                if (version == 1)
                {
                    DeserializeForOneTimestampV1(reader, definition, sparseData, values, sampleTyesWithMetricValueTypeLong, priorValidValues, scalingFactor);
                }
                else
                {
                    DeserializeForOneTimestampV2AndAbove(version, bitReader, definition, values, d, currentBlockLeadingZeros, currentBlockTrailingZeros, priorValidValues);
                }
            }

            // Fill the remaining missing data points at the tail of the series.
            if (sparseData)
            {
                if (values[0].Count < totalNumberOfDatapoints)
                {
                    var numNullsToFill = totalNumberOfDatapoints - values[0].Count;
                    FillNulls((uint)numNullsToFill, values);
                }
            }

            // Start time can be adjusted for distinct count metric or when rollup serivce is enabled.
            var deltaOfStartTimeInMinutes = SerializationUtils.ReadInt32FromBase128(reader);

            // resolution window can be adjusted for distinct count metric or when rollup serivce is enabled.
            var deltaOfResolutionWindowInMinutes = SerializationUtils.ReadInt32FromBase128(reader);

            return new DeserializedRawData(deltaOfStartTimeInMinutes, deltaOfResolutionWindowInMinutes, values, TimeSeriesErrorCode.Success);
        }

        /// <summary>
        /// Deserializes for one timestamp - v1.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="sparseData">if set to <c>true</c> [sparse data].</param>
        /// <param name="values">The metric values.</param>
        /// <param name="sampleTyesWithMetricValueTypeLong">The sample tyes with metric value type long.</param>
        /// <param name="priorValidValues">The prior valid values.</param>
        /// <param name="scalingFactor">The scaling factor.</param>
        private static void DeserializeForOneTimestampV1(
            BinaryReader reader,
            TimeSeriesDefinition<MetricIdentifier> definition,
            bool sparseData,
            List<List<double?>> values,
            bool[] sampleTyesWithMetricValueTypeLong,
            double[] priorValidValues,
            uint scalingFactor)
        {
            // For sparse data, although we don't serialize null values but we do serialize the number of null values between two serialized data points
            // so that we can restore those null values on the client side.
            if (sparseData)
            {
                var numMissingDatapointsSinceLastOne = SerializationUtils.ReadUInt32FromBase128(reader);

                FillNulls(numMissingDatapointsSinceLastOne, values);
            }

            for (int samplingTypeIndex = 0; samplingTypeIndex < values.Count; samplingTypeIndex++)
            {
                if (sampleTyesWithMetricValueTypeLong[samplingTypeIndex])
                {
                    // variable length encoding with differences.
                    var delta = SerializationUtils.ReadInt64FromBase128(reader);
                    if (delta == long.MaxValue)
                    {
                        // Padding
                        values[samplingTypeIndex].Add(null);
                    }
                    else
                    {
                        // We request unscaled values from server and apply the scaling factor on the client.
                        var unScaledValue = delta + priorValidValues[samplingTypeIndex];

                        if (IsCountSamplingType(definition, samplingTypeIndex))
                        {
                            values[samplingTypeIndex].Add(unScaledValue);
                        }
                        else
                        {
                            values[samplingTypeIndex].Add(unScaledValue / scalingFactor);
                        }

                        priorValidValues[samplingTypeIndex] = unScaledValue;
                    }
                }
                else
                {
                    // We don't encode double in v1, so even a delta is still 8 bytes.
                    var delta = reader.ReadDouble();
                    if (double.IsNaN(delta))
                    {
                        // Padding
                        values[samplingTypeIndex].Add(null);
                    }
                    else
                    {
                        var unScaledValue = delta + priorValidValues[samplingTypeIndex];

                        if (IsCountSamplingType(definition, samplingTypeIndex))
                        {
                            values[samplingTypeIndex].Add(delta + priorValidValues[samplingTypeIndex]);
                        }
                        else
                        {
                            values[samplingTypeIndex].Add((delta + priorValidValues[samplingTypeIndex]) / scalingFactor);
                        }

                        priorValidValues[samplingTypeIndex] = unScaledValue;
                    }
                }
            }
        }

        /// <summary>
        /// Deserializes for one timestamp - v2.
        /// </summary>
        /// <param name="version">The serialization version.</param>
        /// <param name="reader">The bit reader.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="values">The values.</param>
        /// <param name="dataPointIndex">Index of the data point.</param>
        /// <param name="currentBlockLeadingZeros">The current block leading zeros.</param>
        /// <param name="currentBlockTrailingZeros">The current block trailing zeros.</param>
        /// <param name="priorValidValues">The prior valid values.</param>
        private static void DeserializeForOneTimestampV2AndAbove(byte version, BitBinaryReader reader, TimeSeriesDefinition<MetricIdentifier> definition, List<List<double?>> values, int dataPointIndex, sbyte[] currentBlockLeadingZeros, sbyte[] currentBlockTrailingZeros, double[] priorValidValues)
        {
            var numSamplingTypes = values.Count;
            for (var s = 0; s < numSamplingTypes; s++)
            {
                if (dataPointIndex == 0)
                {
                    // very first value of the series
                    priorValidValues[s] = reader.BinaryReader.ReadDouble();
                    values[s].Add(GetNullableDouble(priorValidValues[s]));
                }
                else
                {
                    var firstBit = reader.ReadBit();
                    if (!firstBit)
                    {
                        // first bit is 0
                        values[s].Add(GetNullableDouble(priorValidValues[s]));
                    }
                    else
                    {
                        var secondBit = reader.ReadBit();

                        long meaningfulBits;
                        if (!secondBit)
                        {
                            // 2nd bit is 0 while the first is 1.
                            if (currentBlockLeadingZeros[s] < 0)
                            {
                                throw new Exception("The block has not been set so it is a bug in serialization on server");
                            }

                            var numBitsToRead = BitAggregateMagic.NumBitsInLongInteger - currentBlockLeadingZeros[s] - currentBlockTrailingZeros[s];

                            meaningfulBits = reader.ReadBits(numBitsToRead);
                        }
                        else
                        {
                            // a new block position was started since the number starts with "11".
                            currentBlockLeadingZeros[s] = (sbyte)reader.ReadBits(GetNumBitsToEncodeNumLeadingZeros(version));
                            var numBitsToRead = (sbyte)reader.ReadBits(NumBitsToEncodeNumMeaningfulBits);
                            if (numBitsToRead == 0)
                            {
                                // The block size is 64 bits which becomes 0 in writing into 6 bits - overflow.
                                // If the block size were indeed 0 bits, the xor value would be 0, and the actual value would be identical to the prior value,
                                // so we would not have reached here since firstBit would be 0.
                                numBitsToRead = (sbyte)BitAggregateMagic.NumBitsInLongInteger;
                            }

                            currentBlockTrailingZeros[s] = (sbyte)(BitAggregateMagic.NumBitsInLongInteger - currentBlockLeadingZeros[s] - numBitsToRead);

                            meaningfulBits = reader.ReadBits(numBitsToRead);
                        }

                        long xor = meaningfulBits << currentBlockTrailingZeros[s];
                        priorValidValues[s] = BitConverter.Int64BitsToDouble(xor ^ BitConverter.DoubleToInt64Bits(priorValidValues[s]));
                        values[s].Add(GetNullableDouble(priorValidValues[s]));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the nullable double.
        /// </summary>
        /// <param name="priorValidValue">The prior valid value.</param>
        /// <returns>The nullable double.</returns>
        private static double? GetNullableDouble(double priorValidValue)
        {
            return double.IsPositiveInfinity(priorValidValue) ? (double?)null : priorValidValue;
        }

        /// <summary>
        /// Determines whether it is count sampling type.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="samplingTypeIndex">The sampling type index.</param>
        /// <returns>True if it is count sampling type; false otherwise.</returns>
        private static bool IsCountSamplingType(TimeSeriesDefinition<MetricIdentifier> definition, int samplingTypeIndex)
        {
            return (definition == null && samplingTypeIndex == 0) || (definition != null && definition.SamplingTypes[samplingTypeIndex].Equals(SamplingType.Count));
        }

        /// <summary>
        /// Fills the nulls for missing data points.
        /// </summary>
        /// <param name="numMissingDatapointsSinceLastOne">The number missing datapoints since last one.</param>
        /// <param name="values">The values.</param>
        private static void FillNulls(uint numMissingDatapointsSinceLastOne, List<List<double?>> values)
        {
            var numSamplingTypes = values.Count;

            for (int m = 0; m < numMissingDatapointsSinceLastOne; ++m)
            {
                for (int index = 0; index < numSamplingTypes; index++)
                {
                    values[index].Add(null);
                }
            }
        }

        /// <summary>
        /// A private stuct holding the deserialized raw data for a time series.
        /// </summary>
        private struct DeserializedRawData
        {
            internal DeserializedRawData(int deltaOfStartTimeInMinutes, int deltaOfSeriesResolutionInMinutes, List<List<double?>> values, TimeSeriesErrorCode errorCode)
            {
                this.DeltaOfStartTimeInMinutes = deltaOfStartTimeInMinutes;
                this.DeltaOfSeriesResolutionInMinutes = deltaOfSeriesResolutionInMinutes;
                this.Values = values;
                this.ErrorCode = errorCode;
            }

            internal int DeltaOfStartTimeInMinutes { get; }

            internal int DeltaOfSeriesResolutionInMinutes { get; }

            /// <summary>
            /// The raw time series values returned from metric server.
            /// </summary>
            internal List<List<double?>> Values { get; }

            /// <summary>
            /// Gets the error code.
            /// </summary>
            internal TimeSeriesErrorCode ErrorCode { get; }
        }
    }
}
