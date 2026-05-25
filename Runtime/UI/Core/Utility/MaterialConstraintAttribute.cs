#nullable enable
using System;
using System.Diagnostics;
#if UNITY_EDITOR
using System.Collections;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;
using UnityEngine.UI;

[assembly: RegisterValidator(typeof(MaterialConstraintValidator))]
[assembly: RegisterValidator(typeof(MaterialConstraintValidator_List))]
#endif

[Conditional("UNITY_EDITOR")]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
public class MaterialConstraintAttribute : Attribute
{
    public readonly GraphicMaterialKind[] Allowed;

    public MaterialConstraintAttribute(params GraphicMaterialKind[] allowed)
    {
        Allowed = allowed;
    }
}

#if UNITY_EDITOR
internal class MaterialConstraintValidator : AttributeValidator<MaterialConstraintAttribute, Graphic>
{
    protected override void Validate(ValidationResult result)
    {
        var g = ValueEntry.SmartValue;
        if (!g) return;
        ValidateGraphic(g, Attribute.Allowed, result);
    }

    internal static void ValidateGraphic(Graphic g, GraphicMaterialKind[] allowed, ValidationResult result)
    {
        var actual = g.material;
        if (Array.IndexOf(allowed, actual) < 0)
            result.AddError($"Material must be {string.Join(" or ", allowed)} (got {actual}).");
    }
}

internal class MaterialConstraintValidator_List : AttributeValidator<MaterialConstraintAttribute>
{
    protected override void Validate(ValidationResult result)
    {
        var value = Property.ValueEntry.WeakSmartValue;
        if (value is not IList list) return;
        var allowed = Attribute.Allowed;
        foreach (var item in list)
        {
            if (item is Graphic g && g)
                MaterialConstraintValidator.ValidateGraphic(g, allowed, result);
        }
    }
}
#endif
