using UnityEditor;
using UnityEngine;

namespace CardCreator.Editor
{
    // Card artwork is exported at print resolution; block compression and mipmaps soften ink and fine details.
    public sealed class CardArtworkImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/CardCreator/Illustrations/") &&
                !assetPath.StartsWith("Assets/CardCreator/OnlineAssets/") &&
                !assetPath.StartsWith("Assets/CardCreator/EnhancedArtwork/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
        }
    }
}
