using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using VRage.ModAPI;
using VRageMath;
using VRage.Game;
using VRage.Game.ObjectBuilders;

[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
public class SpacelinkDishMod : MySessionComponentBase
{
    private List<IMyTerminalBlock> _dishes = new List<IMyTerminalBlock>();
    private int _tickCounter = 0;
    private float _breathPhase = 0f;
    private const float BreathSpeed = 1f;
    private const float FallbackBroadcastRange = 3000f;

    public override void UpdateBeforeSimulation()
    {
        _tickCounter++;
        if (_tickCounter % 600 == 0)
        {
            RefreshDishes();
        }
        _breathPhase += (float)(Math.PI * 2 * BreathSpeed * (1.0 / 60.0));
        float intensity = (float)(0.5 + 0.5 * Math.Sin(_breathPhase));
        foreach (var dish in _dishes)
        {
            if (dish == null)
                continue;
            var functional = dish as IMyFunctionalBlock;
            if (functional == null || !functional.Enabled)
            {
                RevertToDefaultEmissive(dish);
                continue;
            }
            if (EnemyInRange(dish))
            {
                Color orangeBreath = new Color((int)(255 * intensity), (int)(165 * intensity), 0);
                dish.SetEmissiveParts("Emissive", orangeBreath, 1.0f);
            }
            else
            {
                RevertToDefaultEmissive(dish);
            }
        }
    }

    private void RefreshDishes()
    {
        _dishes.Clear();
        HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
        MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);
        foreach (var entity in entities)
        {
            IMyCubeGrid grid = entity as IMyCubeGrid;
            if (grid == null)
                continue;
            List<IMySlimBlock> slimBlocks = new List<IMySlimBlock>();
            grid.GetBlocks(slimBlocks, b => b.FatBlock != null && b.FatBlock.BlockDefinition.SubtypeName.Equals("Spacelink_Dish", StringComparison.OrdinalIgnoreCase));
            grid.GetBlocks(slimBlocks, b => b.FatBlock != null && b.FatBlock.BlockDefinition.SubtypeName.Equals("Spacelink_Dish_Small", StringComparison.OrdinalIgnoreCase));
            foreach (var slim in slimBlocks)
            {
                IMyTerminalBlock block = slim.FatBlock as IMyTerminalBlock;
                if (block != null)
                    _dishes.Add(block);
            }
        }
    }

    private bool EnemyInRange(IMyTerminalBlock dish)
    {
        float broadcastRange = FallbackBroadcastRange;
        var antenna = dish as IMyRadioAntenna;
        if (antenna != null)
        {
            broadcastRange = antenna.Radius;
        }
        BoundingSphereD sphere = new BoundingSphereD(dish.GetPosition(), broadcastRange);
        HashSet<IMyEntity> nearbyEntities = new HashSet<IMyEntity>();
        MyAPIGateway.Entities.GetEntities(nearbyEntities, e => sphere.Contains(e.GetPosition()) != ContainmentType.Disjoint);
        foreach (var entity in nearbyEntities)
        {
            IMyCubeGrid grid = entity as IMyCubeGrid;
            if (grid != null)
            {
                if (grid == dish.CubeGrid)
                    continue;
                if (IsEnemy(dish, grid))
                    return true;
            }
        }
        return false;
    }

    private bool IsEnemy(IMyTerminalBlock dish, IMyCubeGrid otherGrid)
    {
        var dishOwners = dish.CubeGrid.BigOwners;
        var otherOwners = otherGrid.BigOwners;
        if ((dishOwners == null || dishOwners.Count == 0) && (otherOwners != null && otherOwners.Count > 0))
            return true;
        if (otherOwners == null || otherOwners.Count == 0)
            return true;
        foreach (var owner in otherOwners)
        {
            if (dishOwners.Contains(owner))
                return false;
        }
        return true;
    }

    private void RevertToDefaultEmissive(IMyTerminalBlock dish)
    {
        var functional = dish as IMyFunctionalBlock;
        bool isOn = (functional != null && functional.Enabled);
        Color defaultColor = isOn ? new Color(0, 255, 0) : new Color(255, 0, 0);
        dish.SetEmissiveParts("Emissive", defaultColor, 1.0f);
    }

    protected override void UnloadData()
    {
        _dishes.Clear();
    }
}
