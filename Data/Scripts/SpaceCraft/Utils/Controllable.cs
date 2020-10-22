using SpaceCraft.Utils;
using System;
using System.Collections.Generic;
using Sandbox.Game.World;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using VRage;
using VRageMath;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
//using IMyControllableEntity = Sandbox.Game.Entities.IMyControllableEntity;
using IMyControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;

namespace SpaceCraft.Utils {

  //[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
  //public class Controllable : MySessionComponentBase, IMyEntityController {
  public class Controllable {

    public Order CurrentOrder;
    public List<Order> OrderQueue = new List<Order>();
    public IMyEntity Entity;

    public bool Flying = false;
    public bool Spacecraft = false;
    public bool Drills = false;
    public bool Welders = false;
    public bool Griders = false;
    public bool Wheels = false;
    public bool Cargo = false;
    public bool Destroyed = false;
    public bool Fighter = false;
    public bool IsFactory = false;
    public bool IsRefinery = false;
    public Faction Owner;


    public virtual bool IsStatic
		{
			get
			{
				 return false;
			}
		}

    public VRage.MyFixedPoint MaxVolume {
      get {
        VRage.MyFixedPoint vol = (VRage.MyFixedPoint)0;
        List<IMyInventory> inventories = GetInventory();
        foreach( IMyInventory inv in inventories ) {
          vol += inv.MaxVolume;
        }
        return vol;
      }
    }

    public VRage.MyFixedPoint CurrentVolume {
      get {
        VRage.MyFixedPoint vol = (VRage.MyFixedPoint)0;
        List<IMyInventory> inventories = GetInventory();
        foreach( IMyInventory inv in inventories ) {
          vol += inv.CurrentVolume;
        }
        return vol;
      }
    }

    public int PercentFull {
      get {
        VRage.MyFixedPoint current = (VRage.MyFixedPoint)0;
        VRage.MyFixedPoint max = (VRage.MyFixedPoint)0;
        List<IMyInventory> inventories = GetInventory();
        foreach( IMyInventory inv in inventories ) {
          current += inv.CurrentVolume;
          max += inv.MaxVolume;
        }
        if( max.ToIntSafe() == 0 ) return 0;
        return current.ToIntSafe() / max.ToIntSafe();
      }
    }

    public VRage.MyFixedPoint AvailableVolume {
      get {
        VRage.MyFixedPoint current = (VRage.MyFixedPoint)0;
        VRage.MyFixedPoint max = (VRage.MyFixedPoint)0;
        List<IMyInventory> inventories = GetInventory();
        foreach( IMyInventory inv in inventories ) {
          current += inv.CurrentVolume;
          max += inv.MaxVolume;
        }
        return max - current;
      }
    }

    public int Prioritize( IMyCharacter character ) {
      return 1000;
    }

    public int Prioritize( IMyCubeGrid grid ) {
      return 999;
    }

    public int Prioritize( IMySlimBlock slim ) {
			if( slim.FatBlock == null ) return 0;
      IMyCubeBlock block = slim.FatBlock;
      //BlockDefinition

      //MyCubeBlockDefinition GetCubeBlockDefinition (MyDefinitionId id)
      //MyObjectBuilder_DefinitionBase def = MyDefinitionManager.Static.GetObjectBuilder(slim.BlockDefinition);
      //MyCubeBlockDefinition def = MyDefinitionManager.Static.GetCubeBlockDefinition(slim.BlockDefinition.Id);
      string subtypeName = slim.BlockDefinition.Id.SubtypeName;

      if( block is IMyReactor ) {
        return 200;
      }

      if( block is IMyBatteryBlock ) {
        return 101;
      }

      if( block is IMySolarPanel || subtypeName == "LargeBlockWindTurbine" ) {
        return 100;
      }

      if( block is IMyRefinery ) {
        //return 48;
        return subtypeName == "LargeRefinery" ? 99 : 50;
      }

      if( block is IMyAssembler ) {
        //MyAPIGateway.Utilities.ShowMessage( "Prioritize", "slim.BlockDefinition.DisplayNameString" + slim.BlockDefinition.DisplayNameString );
        //switch( slim.BlockDefinition.DisplayNameString ) {
        switch(subtypeName) {
          case "LargeAssembler":
            return 98;
          case "BasicAssembler":
            return 49;
        }
        return 48;
      }



      if( block is IMyProductionBlock ) {
        return 47;
      }

      if( block is IMyShipDrill ) {
        return 45;
      }

      return 1;
		}

    public Dictionary<string,int> GetSurplus( Dictionary<string,int> surplus = null) {
			if( surplus == null ) surplus = new Dictionary<string,int>();
			List<IMyInventory> inventories = GetInventory();
			foreach( IMyInventory inventory in inventories ) {
				List<IMyInventoryItem> items = inventory.GetItems();
				foreach( IMyInventoryItem item in items ) {
          if( item.Content.TypeId != OBTypes.Component ) continue;
					if( surplus.ContainsKey(item.Content.SubtypeName) ) {
						surplus[item.Content.SubtypeName] += item.Amount.ToIntSafe();
					} else {
						surplus.Add(item.Content.SubtypeName, item.Amount.ToIntSafe());
					}
				}
			}
			return surplus;
		}

    public void Stop() {
      OrderQueue.Clear();
      CurrentOrder = null;
    }

    public virtual bool Execute( Order order, bool force = false ) {

      //MyAPIGateway.Utilities.ShowMessage( "Execute", ToString() + ": " + order.ToString() );

      if( force ) Stop();
      if( order == null ) {
        CurrentOrder = null;
        return false;
      }

      if( CurrentOrder == null ) {
        CurrentOrder = order;
        return true;
      } else {
        OrderQueue.Add(order);
        return true;
      }

      return false;

    }

    public virtual bool Move() {
      if( CurrentOrder.Target == null && CurrentOrder.Destination == null ) {
        CurrentOrder = null;
        return false;
      }

      //Vector3D destination = CurrentOrder.Target == null ? CurrentOrder.Destination : CurrentOrder.Target.WorldMatrix.GetDirectionVector(Base6Directions.Direction.Forward);
      // Vector3D destination = MyAPIGateway.Session.Player.GetPosition();
      // (Entity as Sandbox.Game.Entities.IMyControllableEntity).MoveAndRotate( Vector3.Normalize(destination), Vector2.Zero, 0.0f );
      // Entity.MoveAndRotate( Vector3.Normalize(destination), Vector2.Zero, 0.0f );
      return false;
		}

    public virtual void BeginShoot( MyShootActionEnum action ) {
      ((Sandbox.Game.Entities.IMyControllableEntity)Entity).BeginShoot(action);
    }

    public Order Next() {
      CurrentOrder = null;
      Order o = null;
      if( OrderQueue.Count > 0 ) {
        o = OrderQueue[0];
        OrderQueue.Remove(o);
      } else {
        o = Owner.NeedsOrder(this);
      }

      if( o != null )
        Execute( o );

      return o;

    }

    private void Scan() {
      // MyPlanet planet = Owner.Homeworld;
      // Vector3I c;
      // for (c.Z = 0; c.Z < max.Z; ++c.Z)
      //    for (c.Y = 0; c.Y < max.Y; ++c.Y)
      //       for (c.X = 0; c.X < max.X; ++c.X) {
      //         MyVoxelMaterialDefinition def = planet.GetMaterialAt(ref c);
      //         if( def != null && def.MinedOre != "Stone" ) {
      //           MyAPIGateway.Utilities.ShowMessage( "Material", def.MinedOre );
      //         }
      //       }
    }

    public virtual void Init( MyObjectBuilder_SessionComponent session ) {
			//base.Init(session);
		}

    public virtual void UpdateBeforeSimulation() {
    }

    public virtual List<IMyInventory> GetInventory( List<IMySlimBlock> blocks = null ) {
      return new List<IMyInventory>();
    }

    public void Withdraw() {
      if( CurrentOrder == null || CurrentOrder.Entity == null ) return;
      if( Owner.CurrentGoal.Entity == null) {
        CurrentOrder = null;
        MyAPIGateway.Utilities.ShowMessage( "Withdraw", ToString() + " Owner.CurrentGoal.Entity is null" );
        return;
      }

      if( CurrentOrder.Step == Steps.Pending ) {
        if( !Move() ) {
          CurrentOrder.Entity.Execute( new Order {
            Type = Orders.Deposit,
            Entity = this,
            Target = this.Entity as IMyEntity,
            Range = 500f,
            Filter = OBTypes.Component
          }, true );
          CurrentOrder.Progress();
        }
      } else {

        if( PercentFull >= .9 ) {
          CurrentOrder.Entity.Stop();

          Execute( new Order {
            Type = Orders.Deposit,
            Entity = Owner.CurrentGoal.Entity,
            Target = Owner.CurrentGoal.Entity.Entity as IMyEntity,
            Range = 999999f
          }, true );
        }
      }
    }

    public void Deposit() {
			if( CurrentOrder.Target == null ) {
        if( CurrentOrder.Entity == null )
          CurrentOrder.Entity = Owner.GetClosestGrid(this);
        if( CurrentOrder.Entity == null ) {
          CurrentOrder.Complete();
          return;
        }
        CurrentOrder.Target = (CurrentOrder.Entity as CubeGrid).Grid as IMyEntity;
      }
			if( CurrentOrder.Target == null ) return;

			if( CurrentOrder.Step == Steps.Pending ) {

				if( !Move() ) {
					CurrentOrder.Progress();
				}
			} else {
        // Simple timeout after X attempts
        CurrentOrder.Tick++;
        if( CurrentOrder.Tick == 5000 ) {
          MyAPIGateway.Utilities.ShowMessage( "Deposit", "Gave up after 5000 ticks " + ToString() );
          CurrentOrder = null;
          return;
        }

        if( CurrentOrder.Entity == null ) {
          MyAPIGateway.Utilities.ShowMessage( "Deposit", "Entity was null, giving up " + ToString() );
          CurrentOrder = null;
          return;
        }

        int remaining = 0;
				List<IMyInventory> inventories = GetInventory();

        if( CurrentOrder.Entity is CubeGrid ) {
          CubeGrid grid = CurrentOrder.Entity as CubeGrid;

          // Check for construction
          if( grid.ConstructionSite != null ) {

            foreach( IMyInventory mine in inventories ) {
              grid.ConstructionSite.MoveItemsToConstructionStockpile( mine );
              grid.ConstructionSite.IncreaseMountLevel( 5f, (long)0 );
            }
          }

          if( AreEmpty(inventories) ) {
            CurrentOrder = null;
            return;
          }
        }

        remaining = 0;

        // Normal deposit
				List<IMyInventory> target = CurrentOrder.Entity.GetInventory();
				foreach( IMyInventory inv in target ) {

          foreach( IMyInventory mine in inventories ) {
  					List<IMyInventoryItem> items = mine.GetItems();

  					for( int i = items.Count-1; i >= 0; i-- ) {
              IMyInventoryItem item = items[i];
              if( item.Content.TypeId == OBTypes.Tool ) continue;
              if( CurrentOrder.Filter != MyObjectBuilderType.Invalid && item.Content.TypeId != CurrentOrder.Filter ) continue;
              mine.TransferItemTo(inv, i, null, true, item.Amount, false );
  					}
          }

				}

        if( AreEmpty(inventories) ) {
          CurrentOrder = null;
          return;
        } else {
          // TODO: Tell Grid it needs storage
        }

			}
		}

    public void Drop( MyObjectBuilderType type ) {
      List<IMyInventory> inventories = GetInventory();
      foreach( IMyInventory inventory in inventories ) {
        List<IMyInventoryItem> items = inventory.GetItems();
        for( int i = items.Count -1; i >= 0; i-- ) {
          IMyInventoryItem item = items[i];
          if( item.Content.TypeId == type ) {
            inventory.RemoveItemsAt(i, (VRage.MyFixedPoint)100000);
          }
        }
      }
    }

    public bool AreEmpty( List<IMyInventory> inventories ) {
      foreach( IMyInventory inv in inventories ) {
        if( !IsEmpty(inv) ) return false;
      }
      return true;
    }

    public bool IsEmpty( IMyInventory inventory ) {
      List<IMyInventoryItem> items = inventory.GetItems();
      if( items.Count == 0 ) return true;
      MyObjectBuilderType tools =	MyObjectBuilderType.Parse("MyObjectBuilder_PhysicalGunObject");
      foreach( IMyInventoryItem item in items ) {
        if( item.Content.TypeId != tools ) {
          //MyAPIGateway.Utilities.ShowMessage( "IsEmpty", item.Content.TypeId + " != " + tools );
          return false;
        }
      }
      return true;
    }

    public bool IsEmpty() {
      List<IMyInventory> inventories = GetInventory();
      foreach( IMyInventory inv in inventories ) {
        if( !IsEmpty(inv) ) return false;
      }
      return true;
    }



    // public Vector3D UpVector {
    //   get {
    //     return (position - Entity.WorldMatrix.Translation).Normalize();
    //   }
    // }


    /*public string ToString() {

			try {

				return Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary<Engineer>(this));

			} catch(Exception exc) {

			}

			return string.Empty;

		}*/

  }



}
