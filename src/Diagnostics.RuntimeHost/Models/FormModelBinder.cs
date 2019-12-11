using System;
using System.Threading.Tasks;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Diagnostics.RuntimeHost.Models
{
    public class FormModelBinder : IModelBinder
    {
        /// <summary>
        /// Model Binder used to construct Form object from the query params
        /// </summary>
        Task IModelBinder.BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var formIdValueProvider = bindingContext.ValueProvider.GetValue("fId");
            var inputIdProvider = bindingContext.ValueProvider.GetValue("inpId");
            var inputValueProvider = bindingContext.ValueProvider.GetValue("val");
            var inputTypeProvider = bindingContext.ValueProvider.GetValue("inpType");
            var btnIdProvider = bindingContext.ValueProvider.GetValue("btnId");
            if (formIdValueProvider == ValueProviderResult.None ||
                inputIdProvider == ValueProviderResult.None ||
                inputValueProvider == ValueProviderResult.None ||
                btnIdProvider == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }
            if (!IsInt(formIdValueProvider.FirstValue))
            {
                bindingContext.ModelState.TryAddModelError("fId", "Form Id must be an integer.");
                return Task.CompletedTask;
            }
            if (!IsInt(btnIdProvider.FirstValue))
            {
                bindingContext.ModelState.TryAddModelError("btnId", "Button Id must be an integer.");
                return Task.CompletedTask;
            }
            var result = new Form(Convert.ToInt32(formIdValueProvider.FirstValue));
            result.AddFormInput(new Button(Convert.ToInt32(btnIdProvider.FirstValue), ""));
            if (inputIdProvider.Length != inputValueProvider.Length)
            {
                bindingContext.ModelState.TryAddModelError(
                                       "InputId",
                                       "Number of input ids must be same as number of input values.");
                return Task.CompletedTask;
            }
            var inputIds = inputIdProvider.Values.ToArray();
            var inputValues = inputValueProvider.Values.ToArray();
            var inputTypes = inputTypeProvider.Values.ToArray();
            for (int i = 0; i < inputIds.Length; i++)
            {
                if (!IsInt(inputIds[i]))
                {
                    bindingContext.ModelState.TryAddModelError("inpId", "Input Id must be an integer.");
                    return Task.CompletedTask;
                }
                if (inputValues[i].Length > 150)
                {
                    bindingContext.ModelState.TryAddModelError("val", "Length of val cannot exceed 50 characters.");
                    return Task.CompletedTask;
                }

                if (inputTypes.Length > 0 && i < inputTypes.Length)
                {
                    if (int.TryParse(inputTypes[i], out int intInputType))
                    {
                        FormInputTypes inputType = (FormInputTypes)intInputType;

                        if (inputType == FormInputTypes.TextBox)
                        {
                            var text = new Textbox(Convert.ToInt32(inputIds[i]), "");
                            text.Value = inputValues[i];
                            result.AddFormInput(text);
                        }
                        else if (inputType == FormInputTypes.RadioButton)
                        {
                            var radioButtonList = new RadioButtonList(Convert.ToInt32(inputIds[i]), "");
                            radioButtonList.SelectedValue = inputValues[i];
                            result.AddFormInput(radioButtonList);
                        }
                    }
                }
                else
                {
                    // This is for backward compatibility. This should be removed once the UI is updated.
                    var text = new Textbox(Convert.ToInt32(inputIds[i]), "");
                    text.Value = inputValues[i];
                    result.AddFormInput(text);
                }

                
            }
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        private bool IsInt(string num)
        {
            return int.TryParse(num, out int id);
        }
    }
}
