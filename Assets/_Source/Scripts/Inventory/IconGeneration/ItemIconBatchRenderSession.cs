using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class ItemIconBatchRenderSession : IDisposable
{
    private readonly ItemIconRenderSession _renderSession;

    public ItemIconBatchRenderSession(ItemIconGeneratorSettings settings)
    {
        _renderSession = ItemIconRenderer.CreateSession(settings ?? ItemIconGeneratorSettings.LoadDefault());
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
            ? IconRenderProfile.CreateSlot(itemData, width, height)
            : IconRenderProfile.CreateDefault(itemData, width, height);

        return ItemIconRenderer.RenderIconTextureAsync(itemData, installedModules, renderProfile, _renderSession, false, cancellationToken);
    }

    public void Dispose() => _renderSession.Dispose();
}
