using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class FormContext
    {
       /// <summary>
       /// Form ID of the currently executing form
       /// </summary>
        public int FormId;

        /// <summary>
        /// Button ID of the currently executing form
        /// </summary>
        public int ButtonId;

        /// <summary>
        /// List of inputs to read values from 
        /// </summary>
        public List<ExecuteFormInput> FormInputs;

        public FormContext()
        {
            FormInputs = new List<ExecuteFormInput>();
        }

        public void AddFormInput(ExecuteFormInput executeFormInput)
        {
            FormInputs.Add(executeFormInput);
        }
    }

    public class ExecuteFormInput
    {
        // Input Id 
        public int InputId;

        // Input Value
        public string InputValue;
    }
}
