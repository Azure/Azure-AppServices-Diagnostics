using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Diagnostics.ModelsAndUtils.Models;

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
            var btnIdProvider = bindingContext.ValueProvider.GetValue("btnId");
            if (formIdValueProvider == ValueProviderResult.None || 
                inputIdProvider == ValueProviderResult.None || 
                inputValueProvider == ValueProviderResult.None ||
                btnIdProvider == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }
            if(!IsInt(formIdValueProvider.FirstValue))
            {
                bindingContext.ModelState.TryAddModelError("fId", "Form Id must be an integer.");
                return Task.CompletedTask;
            }
            if(!IsInt(btnIdProvider.FirstValue))
            {
                bindingContext.ModelState.TryAddModelError("btnId", "Button Id must be an integer.");
                return Task.CompletedTask;
            }
            var result = new FormContext();
            result.FormId = Convert.ToInt32(formIdValueProvider.FirstValue);
            result.ButtonId = Convert.ToInt32(btnIdProvider.FirstValue);
            if(inputIdProvider.Length != inputValueProvider.Length)
            {
                bindingContext.ModelState.TryAddModelError(
                                       "InputId",
                                       "Number of input ids must be same as number of input values.");
                return Task.CompletedTask;
            }
            var inputIds = inputIdProvider.Values.ToArray();
            var inputValues = inputValueProvider.Values.ToArray();
            for(int i = 0; i<inputIds.Length; i++)
            {
                if(!IsInt(inputIds[i]))
                {
                    bindingContext.ModelState.TryAddModelError("inpId", "Input Id must be an integer.");
                    return Task.CompletedTask;
                }
                if(inputValues[i].Length > 50)
                {
                    bindingContext.ModelState.TryAddModelError("val", "Length of val cannot exceed 50 characters.");
                    return Task.CompletedTask;
                }
                result.AddFormInput(new ExecuteFormInput
                {
                    InputId = Convert.ToInt32(inputIds[i]),
                    InputValue = inputValues[i]
                });
            }
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        private bool IsInt(string num)
        {
            int id = 0;
            return int.TryParse(num, out id);
        }
    }
}
