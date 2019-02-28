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
        public List<FormInputBase> FormInputs;

        /// <summary>
        /// Creates instance of Form class
        /// </summary>
        /// <param name="id">Unique ID for the Form</param>
        /// <param name="title">Title for the Form</param>
        public Form(int id, string title = "")
        {
            FormId = id;
            FormInputs = new List<FormInputBase>();
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
        public void AddFormInput(FormInputBase input)
        {
            // Returns true if we are able to add the input id to hashset
            if(CurrentInputIds.Add(input.InputId))
            {
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
        public void AddFormInputs(List<FormInputBase> inputs)
        {
            inputs.ForEach(input =>
            {
                AddFormInput(input);
            });
        }
    }

    public abstract class FormInputBase
    {
        /// <summary>
        /// Represents Input ID
        /// </summary>
        public int InputId;

        /// <summary>
        /// Represents the Input Type
        /// </summary>
        public FormInputTypes InputType;

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
        /// <param name="label">The label of the input</param>
        /// <param name="isRequired">Indicates whether this is a required input</param>
        public FormInputBase(int id, FormInputTypes inputType, string label, bool isRequired = false)
        {
            InputId = id;
            InputType = inputType;
            Label = label;
            IsRequired = isRequired;
        }
    }

    /// <summary>
    /// Textbox class used to create a textbox input
    /// </summary>
    /// <example>
    /// This sample shows how to create a textbox input
    /// <code>
    /// Textbox input1 = new Textbox(1, "Role Instance name", true);
    /// </code>
    /// </example>
    public class Textbox: FormInputBase
    {
        /// <summary>
        /// Value of the textbox
        /// </summary>
        public string Value;

        /// <summary>
        /// Creates an instance of Textbox class
        /// </summary>
        /// <param name="id">Unique id for the input</param>
        /// <param name="label">Label for the textbox</param>
        /// <param name="isRequired">Indicates if it is a required input</param>
        public Textbox(int id, string label, bool isRequired = false): base(id, FormInputTypes.TextBox, label, isRequired)
        {
        }
    }

    /// <summary>
    /// Button class used to create a button input
    /// </summary>
    /// <example>
    /// This sample shows how to create a button input
    /// <code>
    /// Button saveButton = new Button(1, "Save");
    /// </code>
    /// </example>
    public class Button: FormInputBase
    {
        /// <summary>
        /// Sets the bootstrap button style
        /// </summary>
        public ButtonStyles ButtonStyle;

        /// <summary>
        /// Creates an instance of button class 
        /// </summary>
        /// <param name="id">Unique id for the button</param>
        /// <param name="label">Label to display on the button</param>
        /// <param name="buttonStyle">Bootstrap button style for the button</param>
        public Button(int id, string label, ButtonStyles buttonStyle = ButtonStyles.Primary): base(id, FormInputTypes.Button, label, false)
        {
            this.ButtonStyle = buttonStyle;
        }

    }

    public enum FormInputTypes
    {
        TextBox = 0,
        Checkbox,
        RadioButton,
        DropDown,
        Button
    }

    public enum ButtonStyles
    {
        Primary = 0,
        Secondary,
        Success,
        Danger,
        Warning,
        Info,
        Light,
        Dark,
        Link
    }

    public static class ResponseFormExtension
    {
        /// <summary>
        /// Adds a list of forms to response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="forms">List of forms</param>
        /// <example>
        /// This sample shows how to use <see cref="AddForms"/> method to add a list of Form to the response.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res) 
        /// {
        ///     Form myform1 = new Form(1);
        ///     Textbox input1 = new Textbox(1, "Enter input 1", true);
        ///     Button saveButton = new Button(2, "Save");
        ///     myform1.AddFormInputs(new List<![CDATA[<FormInput]]>() { input1, saveButton});
        ///     Form myform2 = new Form(2);
        ///     Textbox input2 = new Textbox(1, "Enter input 2", true);
        ///     Button saveButton2 = new Button(2, "Save");
        ///     myform2.AddFormInputs(new List<![CDATA[<FormInput]]>() { input2, saveButton2});
        ///     res.AddForms(new List<![CDATA[<Form]]>() { myform1, myform2});
        /// }
        /// </code>
        /// </example>
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
            table.Columns.Add(new DataColumn("Inputs", typeof(List<FormInputBase>)));
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
        /// <example>
        /// This sample shows how to use <see cref="AddForm"/> method to add a Form to the response.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res) 
        /// {
        ///     Form myform = new Form(1);
        ///     Textbox input1 = new Textbox(1, "Enter input", true);
        ///     Button saveButton = new Button(2, "Save");
        ///     myform.AddFormInputs(new List<![CDATA[<FormInput]]>() { input1, saveButton});
        ///     res.AddForm(myform);
        /// }
        /// </code>
        /// </example>
        public static DiagnosticData AddForm(this Response response, Form form)
        {
            return AddForms(response, new List<Form> { form });
        }
    }
}
