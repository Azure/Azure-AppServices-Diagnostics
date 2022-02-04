using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            if (CurrentInputIds.Add(input.InputId))
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
        public int InputId { get; set; }

        /// <summary>
        /// Represents the Input Type
        /// </summary>
        public FormInputTypes InputType { get; set; }

        /// <summary>
        /// Represents whether this is a required input
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Represents the label of the input
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Tooltip for the label of the input
        /// </summary>
        public string ToolTip { get; set; }

        /// <summary>
        /// Tooltip icon
        /// </summary>
        public string TooltipIcon { get; set; }

        /// <summary>
        /// Sets the default visibility of the forminput
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Creates an input with given id and input type
        /// </summary>
        /// <param name="id">unique id for the input</param>
        /// <param name="inputType">Input type for the input</param>
        /// <param name="label">The label of the input</param>
        /// <param name="isRequired">Indicates whether this is a required input</param>
        public FormInputBase(int id, FormInputTypes inputType, string label, bool isRequired = false) : this(id, inputType, label, string.Empty, string.Empty, isRequired)
        {

        }

        public FormInputBase(int id, FormInputTypes inputType, string label, string tooltip, string tooltipIcon = "", bool isRequired = false)
        {
            InputId = id;
            InputType = inputType;
            Label = label;
            IsRequired = isRequired;
            ToolTip = tooltip;
            TooltipIcon = tooltipIcon;
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
    public class Textbox : FormInputBase
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
        public Textbox(int id, string label, bool isRequired = false) : this(id, label, string.Empty, string.Empty, isRequired)
        {
        }

        public Textbox(int id, string label, string tooltip, string tooltipIcon = "", bool isRequired = false) : base(id, FormInputTypes.TextBox, label, tooltip, tooltipIcon, isRequired)
        {

        }
    }

    /// <summary>
    /// RadioButtonList class used to add a list of radio buttons to a form field
    /// </summary>
    /// <example>
    ///  This sample shows how to add a RadioButtonList to a form
    ///  <code>
    ///  
    ///  Form myform = new Form(1);
    /// 
    ///  Button saveButton = new Button(2, "Button Label");
    /// 
    ///  var item1 = new ListItem("First Downtime", "Downtime1", true);
    ///  var item2 = new ListItem("Second Downtime", "Downtime2", false);
    /// 
    ///  var radioList = new RadioButtonList(3, "Select a downtime", new List<![CDATA[<ListItem]]>(){item1, item2});
    /// 
    ///  myform.AddFormInputs(new List<![CDATA[<FormInputBase]]>() { saveButton, radios});
    ///  
    ///  if(cxt.Form != null) 
    ///  {
    ///     RadioButtonList radioInput = (RadioButtonList)Utilities.GetFormInput(cxt.Form, 3);
    ///     var markdown += $"<br/> The value selected in RadioButton is: {radioInput.SelectedValue} ";
    ///     res.AddMarkdownView(markdown, "Form Output");
    ///  }
    /// 
    ///  </code>    
    /// </example>
    public class RadioButtonList : FormInputBase
    {

        /// <summary>
        /// The selected value from the radio button list
        /// </summary>
        public string SelectedValue { get; set; }

        /// <summary>
        /// List of items for this RadioButtonList
        /// </summary>
        public List<ListItem> Items { get; set; }

        /// <summary>
        /// Create an instance of new RadioButtonList class
        /// </summary>
        /// <param name="id">Unique id for the input</param>
        /// <param name="label">Label for the radio button list</param>
        /// <param name="items">The list of items for this list</param>
        public RadioButtonList(int id, string label, List<ListItem> items) : this(id, label, items, string.Empty, string.Empty)
        {
            Items = items;
        }

        public RadioButtonList(int id, string label, List<ListItem> items, string tooltip = "", string tooltipIcon = "") : base(id, FormInputTypes.RadioButton, label, tooltip, tooltipIcon, false)
        {
            Items = items;
        }

    }

    /// <summary>
    /// ListItem class used to create items for RadioButtonList 
    /// </summary>
    public class ListItem
    {
        /// <summary>
        /// The text of the list item
        /// </summary>
        public string Text;

        /// <summary>
        /// The HTML value for the list item
        /// </summary>
        public string Value;

        /// <summary>
        /// Whether this list item is selected or not. If multiple items have
        /// IsSelected set to true, the last item that will be added to the list
        /// will be pre-selected one
        /// </summary>
        public bool IsSelected;

        /// <summary>
        /// Tooltip for list item.
        /// </summary>
        public string Tooltip;

        /// <summary>
        /// Tooltipicon
        /// </summary>
        public string TooltipIcon;


        [JsonPropertyName("text")]
        public string TextSTJCompat
        {
            get
            {
                return Text;
            }
        }

        [JsonPropertyName("value")]
        public string ValueSTJCompat
        {
            get
            {
                return Value;
            }
        }

        [JsonPropertyName("isSelected")]
        public bool IsSelectedSTJCompat
        {
            get
            {
                return IsSelected;
            }
        }

        [JsonPropertyName("toolTip")]
        public string ToolTipSTJCompat
        {
            get
            {
                return Tooltip;
            }
        }

        [JsonPropertyName("toolTipIcon")]
        public string ToolTipIconSTJCompat
        {
            get
            {
                return TooltipIcon;
            }
        }

        /// <summary>
        /// Creates an instance of a new ListItem class
        /// </summary>
        /// <param name="text">Text for the list item</param>
        /// <param name="value">HTML value for the list item</param>
        /// <param name="isSelected">whether the list item is auto selected or not</param>
        public ListItem(string text, string value, bool isSelected = false) : this(text, value, string.Empty, string.Empty, isSelected)
        {
        }

        public ListItem(string text, string value, string tooltip, string tooltipIcon = "", bool isSelected = false)
        {
            Text = text;
            Value = value;
            IsSelected = isSelected;
            Tooltip = tooltip;
            TooltipIcon = tooltipIcon;
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
    public class Button : FormInputBase
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
        public Button(int id, string label, ButtonStyles buttonStyle = ButtonStyles.Primary) : base(id, FormInputTypes.Button, label, false)
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

    public class FormDropdown : FormInputBase
    {
        /// <summary>
        /// Set of options for this dropdown
        /// </summary>
        public List<DropdownOption> DropdownOptions { get; set; }

        /// <summary>
        /// Flag to indicate if multi select is allowed
        /// </summary>
        public bool IsMultiSelect { get; set; }

        /// <summary>
        /// Default selected key. 
        /// </summary>
        public string DefaultSelectedKey { get; set; }

        /// <summary>
        /// Default selected keys in case of MultiSelect
        /// </summary>
        public List<string> DefaultSelectedKeys { get; set; }

        /// <summary>
        /// Values selected by the user
        /// </summary>
        public List<string> SelectedValues { get; set; }

        /// <summary>
        /// List of all children belonging to dropdown
        /// </summary>
        public List<string> Children { get; set; }

        public FormDropdown(int id, string label, List<DropdownOption> options, string defaultKey = "", bool multiSelect = false, List<string> defaultKeys = null, string tooltip = "", string tooltipIcon = "") : base(id, FormInputTypes.DropDown, label, tooltip, tooltipIcon)
        {
            DropdownOptions = options;
            IsMultiSelect = multiSelect;
            DefaultSelectedKey = defaultKey;
            DefaultSelectedKeys = new List<string>();
            if (defaultKeys != null)
            {
                DefaultSelectedKeys.AddRange(defaultKeys);
            }
            Children = new List<string>();
            if (options != null)
            {
                options.ForEach(op =>
                {
                    if (op.Children != null)
                    {
                        Children.AddRange(op.Children);
                    }
                });
            }
        }
    }

    public class DropdownOption
    {
        /// <summary>
        /// Text to render for this option;
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Key associated with this option;
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Whether this item is selected by default.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// List of child input ids.
        /// </summary>
        public List<string> Children { get; set; }

        public DropdownOption(string text, string key, bool isSelected = false, List<string> children = null)
        {
            Text = text;
            Key = key;
            IsSelected = isSelected;
            Children = children;
        }
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
            if (forms == null)
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
