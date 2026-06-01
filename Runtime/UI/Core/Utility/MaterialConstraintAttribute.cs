#nullable enable
using System;
using System.Diagnostics;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine.UI;

[assembly: RegisterValidator(typeof(MaterialConstraintValidator))]
#endif

[Conditional("UNITY_EDITOR")]
[AttributeUsage(AttributeTargets.Class)]
public class MaterialConstraintAttribute : Attribute
{
    public readonly GraphicMaterialKind[] Allowed;

    public MaterialConstraintAttribute(params GraphicMaterialKind[] allowed)
    {
        Allowed = allowed;
    }
}

#if UNITY_EDITOR
internal class MaterialConstraintValidator : AttributeValidator<MaterialConstraintAttribute>
{
    protected override void Validate(ValidationResult result)
    {
        // Class-level attribute, so the validated value is the component itself. A non-Graphic owner
        // can't be auto-validated — it must check the material manually (see UISwfBase).
        if (Property.ValueEntry.WeakSmartValue is not Graphic g)
        {
            result.AddError("[MaterialConstraint] only applies to a Graphic class.");
            return;
        }
        if (!g) return;
        if (Array.IndexOf(Attribute.Allowed, g.material) < 0)
            result.AddError($"Material must be {string.Join(" or ", Attribute.Allowed)} (got {g.material}).");
    }
}
#endif
