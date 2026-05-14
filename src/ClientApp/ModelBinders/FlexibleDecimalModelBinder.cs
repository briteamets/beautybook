using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ClientApp.ModelBinders;

public class FlexibleDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (TryParseDecimal(value, out var result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
            $"The value '{value}' is not valid for {bindingContext.ModelMetadata.DisplayName ?? bindingContext.ModelName}.");
        return Task.CompletedTask;
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        var styles = NumberStyles.Number;
        var cultures = new[]
        {
            CultureInfo.CurrentCulture,
            CultureInfo.InvariantCulture,
            CultureInfo.GetCultureInfo("en-US"),
            CultureInfo.GetCultureInfo("et-EE")
        };

        foreach (var culture in cultures)
        {
            if (decimal.TryParse(value, styles, culture, out result)) return true;
        }

        var normalized = value.Replace(',', '.');
        return decimal.TryParse(normalized, styles, CultureInfo.InvariantCulture, out result);
    }
}

public class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var modelType = context.Metadata.UnderlyingOrModelType;
        return modelType == typeof(decimal) ? new FlexibleDecimalModelBinder() : null;
    }
}
