using System.Collections.Generic;
using System.Data;
namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class Form
    {
        /// <summary>
        /// Represents Form ID
        /// </summary>
        public int FormId;

        /// <summary>
        /// Represents list of inputs for the form
        /// </summary>
        public List<FormInput> FormInputs;

        /// <summary>
        /// Creates instance of Form class
        /// </summary>
        /// <param name="id">Unique ID for the Form</param>
        public Form(int id)
        {
            FormId = id;
            FormInputs = new List<FormInput>();
        }

        /// <summary>
        /// Adds a form input to the form
        /// </summary>
        /// <param name="input"></param>
        public void AddFormInput(FormInput input)
        {
            FormInputs.Add(input);
        }
    }

    public class FormInput
    {
        /// <summary>
        /// Represents Input ID
        /// </summary>
        public int InputId;

        /// <summary>
        /// Represents the Input Type
        /// </summary>
        public InputTypes InputType;

        /// <summary>
        /// Represents whether this is a required input
        /// </summary>
        public bool IsRequired;

        /// <summary>
        /// Represents the label of the input 
        /// </summary>
        public string Label;

        /// <summary>
        /// Creates an input with given id and input type
        /// </summary>
        /// <param name="id">unique id for the input</param>
        /// <param name="inputType">Input type for the input</param>
        /// <param name="isRequired">Indicates whether this is a required input</param>
        /// <param name="label">The label of the input</param>
        public FormInput(int id, InputTypes inputType, bool isRequired = false, string label = "")
        {
            InputId = id;
            InputType = inputType;
            IsRequired = isRequired;
            Label = label;
        }
    }


    public enum InputTypes
    {
        TextBox = 0,
        Checkbox,
        RadioButton,
        DropDown,
    }

    public static class ResponseFormExtension
    {
        public static DiagnosticData AddFormView(this Response response, Form form)
        {
            if(form == null)
            {
                return null;
            }
            var table = new DataTable();
            table.Columns.Add(new DataColumn("FormId", typeof(int)));
            table.Columns.Add(new DataColumn("InputId", typeof(int)));
            table.Columns.Add(new DataColumn("InputType", typeof(InputTypes)));
            table.Columns.Add(new DataColumn("IsRequired", typeof(bool)));
            table.Columns.Add(new DataColumn("Label", typeof(string)));
            
            foreach(var input in form.FormInputs)
            {
                table.Rows.Add(new object[]
                {
                    form.FormId,
                    input.InputId,
                    input.InputType,
                    input.IsRequired,
                    input.Label
                });
            }
            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Form)
                {
                    Title = string.Empty
                }
            };

            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}
