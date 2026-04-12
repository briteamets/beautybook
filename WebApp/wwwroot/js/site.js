// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Accept both "90.00" and "90,00" in decimal inputs. jQuery validation defaults to
// invariant dot decimals, while the app also supports Estonian comma decimals.
if (window.jQuery?.validator) {
    window.jQuery.validator.methods.number = function (value, element) {
        return this.optional(element) || /^-?(?:\d+|\d{1,3}(?:[\s]\d{3})+)(?:[.,]\d+)?$/.test(value);
    };
}
