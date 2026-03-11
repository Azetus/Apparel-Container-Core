using UnityEngine;
using Verse;

namespace ACC_ApparelContainerCore.ACC_Utility;

[StaticConstructorOnStartup]
public class ACC_IconTexture
{
    public static readonly Texture2D VMM_DropAll_Icon = ContentFinder<Texture2D>.Get("ACC_DropAll_Icon");
}