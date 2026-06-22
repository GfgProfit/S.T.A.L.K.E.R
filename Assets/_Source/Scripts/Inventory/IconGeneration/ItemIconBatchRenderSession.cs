using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class ItemIconBatchRenderSession : IDisposable
{
    private readonly ItemIconGeneratorSettings _settings;
    private readonly ItemIconRenderSession _renderSession;

    public ItemIconBatchRenderSession(ItemIconGeneratorSettings settings)
    {
        _settings = settings ?? ItemIconGeneratorSettings.LoadDefault();
        _renderSession = ItemIconRenderer.CreateSession(_settings);
    }

    public UniTask<Texture2D> RenderAsync(
        ItemData itemData,
        IReadOnlyList<ItemData> installedModules,
        int width,
        int height,
        ItemIconProfileType profileType,
        CancellationToken cancellationToken)
    {
        IconRenderProfile renderProfile = profileType == ItemIconProfileType.Slot
            ? IconRenderProfile.CreateSlot(itemData, width, height, _settings)
            : IconRenderProfile.CreateDefault(itemData, width, height, _settings);

        return ItemIconRenderer.RenderIconTextureAsync(itemData, installedModules, renderProfile, _renderSession, false, cancellationToken);
    }

    public void Dispose() => _renderSession.Dispose();
}
