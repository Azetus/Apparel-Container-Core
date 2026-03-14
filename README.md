# Apparel-Container-Core

一个 Rimworld Mod 代码库。基于 `ThingComp` 实现，允许服装挂载独立的存储容器。

## 核心功能：

**服装容器：** 挂载了 Mod `ThingComp` 的服装现在拥有内部存储空间。

**库存管理面板：** 一个管理服装内部库存的UI面板。

**Gizmo代理：** 可以将功能性装备（如便携护盾背包、心灵冲击枪等）直接存入附包中，并像正常装备一样在操作栏直接使用它们。

**白名单与黑名单：** 可以在Mod设置中调整存入物品的白名单与黑名单。

## 兼容性：

理论上兼容绝大部分 Mod 物品，但个别特殊装备的功能按钮（Gizmo）可能无法在代理状态下使用。

## 如何使用：

1. 在你的 Mod 的 `About.xml` 中将本 Mod 添加为依赖。并设置加载顺序。
   ```xml
   <modDependencies>
       <li>
           <packageId>Aliza.ApparelContainerCore</packageId>
           <displayName>ApparelContainerCore</displayName>
           <steamWorkshopUrl>https://steamcommunity.com/sharedfiles/filedetails/?id=3682926769</steamWorkshopUrl>
           <downloadUrl>https://github.com/Azetus/Apparel-Container-Core</downloadUrl>
       </li>
   </modDependencies>
   ```
   ```xml
   <loadAfter>
       <li>Ludeon.RimWorld</li>
       <li>Aliza.ApparelContainerCore</li>
   </loadAfter>
   ```
2. 编写你的 `ThingDef` xml文件，确保这个`Def`的`thingClass`是`Apparel`或是其子类。
   ```xml 
   <thingClass>Apparel</thingClass>
   ```
3. 设置 `ThingDef` 的 `tickerType` 标签为 `Normal`，以确保容器内装备的`Ability`能够正常冷却。
   ```xml
   <tickerType>Normal</tickerType>
   ```
4. 然后在`ThingDef`的`comps`标签内引用本Mod添加的`CompProperties_GenericPackForApparel`，并在`storageCapacity`标签内填入容器的最大容量。
   ```xml
   <comps>
      <li Class="ACC_ApparelContainerCore.Comps.Props.CompProperties_GenericPackForApparel">
         <storageCapacity>3</storageCapacity>
      </li>
   </comps>
   ```

# Apparel-Container-Core

A RimWorld framework library based on `ThingComp` that allows apparel to host independent storage containers.

## Core Features:

**Apparel Containers:** Apparel equipped with the mod's `ThingComp` now have internal storage space.

**Inventory Management UI:** A UI panel for managing the internal inventory of the apparel.

**Gizmo Proxying:** Utility equipment (such as Low-shield pack, Psychic shock lance, etc.) can be stored in pouches and used directly from the pawn's
command bar as if they were equipped normally.

**Whitelist & Blacklist:** Whitelists and blacklists for storable items can be adjusted in the mod settings.

## Compatibility:

Theoretically compatible with most mod items, though Gizmo proxying may not function for certain equipment with unique or non-standard logic.

## How to use:

1. Add this mod as a dependency in your mod's `About.xml` and ensure the correct Load Order.
   ```xml
   <modDependencies>
       <li>
           <packageId>Aliza.ApparelContainerCore</packageId>
           <displayName>ApparelContainerCore</displayName>
           <steamWorkshopUrl>https://steamcommunity.com/sharedfiles/filedetails/?id=3682926769</steamWorkshopUrl>
           <downloadUrl>https://github.com/Azetus/Apparel-Container-Core</downloadUrl>
       </li>
   </modDependencies>
   ```
   ```xml
   <loadAfter>
       <li>Ludeon.RimWorld</li>
       <li>Aliza.ApparelContainerCore</li>
   </loadAfter>
   ```
2. Define your `ThingDef` XML: Ensure that the `thingClass` of your `Def` is `Apparel` or one of its subclasses.
   ```xml
   <thingClass>Apparel</thingClass>
   ```
3. Set the `tickerType` tag of your `ThingDef` to `Normal` to ensure that the `Ability` of the equipment inside the container can cool down properly.
   ```xml
   <tickerType>Normal</tickerType>
   ```
4. Add the Component: Reference the `CompProperties_GenericPackForApparel` added by this mod within the `comps` node of your `ThingDef`. Then, specify
   the maximum container capacity in the `storageCapacity` field.
   ```xml
   <comps>
       <li Class="ACC_ApparelContainerCore.Comps.Props.CompProperties_GenericPackForApparel">
           <storageCapacity>3</storageCapacity>
       </li>
   </comps>
   ```