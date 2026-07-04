using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class SpireSidekickCityFleetBaseInfo : ExternalFleetBaseInfo, IBolsteringFleet
    {
        protected override void Cleanup()
        {
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public List<SafeSquadWrapper> FleetsThatCanBeBolstered {
            get {
                return SpireSidekickFactionBaseInfo.Instance.SpirePlayerFleets.GetDisplayList();
            }
        }

        public override void PerFrame_UpdateFleetData(Faction LocalPlayerFactionForUICalculations)
        {
            GameEntity_Squad city = this.AttachedFleet.Centerpiece.GetSquad();
            if (city != null) {
                this.AttachedFleet.NameSuffix = " " + Balance_MarkLevelTable.Instance.RowsByOrdinal[city.CurrentMarkLevel].MapDisplayWithColor;
            }
            RecalculateSpireCityBuildingContents();
        }

        public void RecalculateSpireCityBuildingContents()
        {
            bool ExpertMode = SpireSidekickFactionBaseInfo.Instance.ExpertMode;
            CityManager.RecalculateCityBuildingContents(this.AttachedFleet, "SpireSidekickCityBuildMenu", delegate (GameEntity_Squad city, GameEntityTypeData buildingInfo) {
                if (!ExpertMode && buildingInfo.GetHasTag("Spire_Expert")) {
                    return false;
                } else if (ExpertMode && buildingInfo.GetHasTag("Spire_Normal")) {
                    return false;
                }
                return true;
            });
        }

    }
}
