using System;
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
        /// Title for the form 
        /// </summary>
        public string FormTitle;

        /// <summary>
        /// Represents list of inputs for the form
        /// </summary>
        public List<FormInput> FormInputs;

        /// <summary>
        /// Creates instance of Form class
        /// </summary>
        /// <param name="id">Unique ID for the Form</param>
        /// <param name="title">Title for the Form</param>
        public Form(int id, string title = "")
        {
            FormId = id;
            FormInputs = new List<FormInput>();
            CurrentInputIds = new HashSet<int>();
            FormTitle = title;
        }

        /// <summary>
        /// Hashset containing input IDs
        /// </summary>
        private HashSet<int> CurrentInputIds;

        /// <summary>
        /// Adds a form input to the form
        /// </summary>
        /// <param name="input">Input element to be added to the form</param>
        public void AddFormInput(FormInput input)
        {
            // Returns true if we are able to add the input id to hashset
            if(CurrentInputIds.Add(input.InputId))
            {
                input.CombinedId = input.InputId.ToString() + "." + FormId.ToString();
                FormInputs.Add(input);             
            }
            else
            {
                throw new Exception($"Input ID {input.InputId} already exists for Form {FormId}. Please give a unique ID for the input");
            }          
        }

        /// <summary>
        /// Adds a list of form inputs to the form
        /// </summary>
        /// <param name="inputs">List of form inputs</param>
        public void AddFormInputs(List<FormInput> inputs)
        {
            inputs.ForEach(input =>
            {
                AddFormInput(input);
            });
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
        /// Combines the parent form id and input id
        /// </summary>
        public string CombinedId;

        /// <summary>
        /// Creates an input with given id and input type
        /// </summary>
        /// <param name="id">unique id for the input</param>
        /// <param name="inputType">Input type for the input</param>
        /// <param name="label">The label of the input</param>
        /// <param name="isRequired">Indicates whether this is a required input</param>
        public FormInput(int id, InputTypes inputType, string label = "", bool isRequired = false)
        {
            InputId = id;
            InputType = inputType;
            Label = label;
            IsRequired = isRequired;
        }
    }

    public enum InputTypes
    {
        TextBox = 0,
        Checkbox,
        RadioButton,
        DropDown,
        Button
    }


    public static class ResponseFormExtension
    {
        /// <summary>
        /// Adds a list of forms to response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="forms">List of forms</param>
        /// <returns></returns>
        public static DiagnosticData AddForms(this Response response, List<Form> forms)
        {
            if(forms == null)
            {
                return null;
            }

            var table = new DataTable();
            table.TableName = "Forms";
            table.Columns.Add(new DataColumn("FormId", typeof(int)));
            table.Columns.Add(new DataColumn("FormTitle", typeof(string)));
            table.Columns.Add(new DataColumn("Inputs", typeof(List<FormInput>)));
            forms.ForEach(form =>
            {               
                table.Rows.Add(new object[]
                {
                    form.FormId,
                    form.FormTitle,
                    form.FormInputs,
                });              
            });

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Form)
                {
                    Title = string.Empty
                }
            };
            response.DetectorForms.AddRange(forms);
            response.Dataset.Add(diagData);
            return diagData;
        }

        /// <summary>
        /// Adds a single form to response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="form">Form to be added</param>
        /// <returns></returns>
        public static DiagnosticData AddForm(this Response response, Form form)
        {
            return AddForms(response, new List<Form> { form });
        }
    }
}
