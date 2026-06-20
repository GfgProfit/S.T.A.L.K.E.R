using UnityEditor.Build;
using UnityEditor.Build.Reporting;

internal sealed class ItemIconBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        ItemIconCatalogValidationResult validation = ItemIconCatalogValidator.Validate(true);

        if (validation.IsValid == false)
        {
            throw new BuildFailedException($"Baked item icon validation failed with {validation.Errors.Count} issue(s). Open Tools/Inventory/Item Icon Generator and run Validate Catalog.");
        }
    }
}
