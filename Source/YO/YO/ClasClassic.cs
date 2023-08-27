using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI.Group;
using UnityEngine;
using Verse.AI;
//using CombatExtended;

namespace YO
{
    public struct DefLine 
    {
        public List<Building> stuff;

        public IntVec3 dir;
    }


    public class Classifier : MapComponent
    {

        public Classifier(Map world) : base(world)
        {
        }

        public List<DefLine> lines;
        public bool wpiriot = false;
        public void FindDefensiveLines()
        {
            var turrets = this.map.listerBuildings.allBuildingsColonist.FindAll(x => x is Building_TurretGun);
            var embrasures = this.map.listerBuildings.allBuildingsColonist.FindAll(x => x?.def?.defName.ToLower().Contains("embras") ?? false);
            var walls = new List<Building>();

            var results = new List<DefLine>();

            var used = new List<Thing>();

            foreach (var emb in embrasures)
            {
                if (!used.Contains(emb))
                {
                    var shit = new List<Building>();

                    foreach (var a in emb.CellsAdjacent8WayAndInside())
                    {
                        shit.Add((Building)a.GetFirstThing(this.map, ThingDefOf.Wall));
                    }

                    var closes = embrasures.FindAll(x => x.AdjacentTo8WayOrInside(emb));


                    if (shit.Any(/*x => x.Faction == Faction.OfPlayer*/))
                    {
                        shit.Add((Building)emb);
                        results.Add(new DefLine() { stuff = shit });
                        used.AddRange(closes);

                    }
                }
            }

            lines = results;
        }
    }

    public class JobGiver_ShturmScout : ThinkNode_JobGiver
    {
        Thing picked = null;

        Classifier mapcomp = null;

        bool check = false;

        VerbProperties kaczka;

        protected override Job TryGiveJob(Pawn pawn)
        {
            //Log.Message("3");
            if (kaczka == null)
            {
                kaczka = pawn.equipment?.PrimaryEq?.PrimaryVerb?.verbProps;
            }

            //Log.Message("33");
            if (mapcomp == null && !check)
            {
                mapcomp = pawn.Map?.GetComponent<Classifier>();
                check = true;
            }

            //Log.Message("333");
            if (mapcomp == null)
            {
                return null;
            }

            //Log.Message("3333");
            if (picked == null)
            {
                //Log.Message("1");
                foreach (var defenceL in mapcomp?.lines?.OrderBy(x => x.stuff[0]?.Position.DistanceTo(pawn.Position)))
                {
                    //Log.Message("2");
                    foreach (var defence in defenceL.stuff?.OrderBy(x => x?.Position.DistanceTo(pawn.Position)).Where(x => x != null))
                    {
                        //Log.Message("fart");
                        if (defence?.Position.DistanceTo(pawn.Position) < 50)
                        {
                            //Log.Message("3-4");
                            if (GenSight.LineOfSightToThing(pawn.Position, defence, pawn.Map))
                            {
                                picked = defence;
                                break;
                            }
                        }
                    }
                    //Log.Message("5");
                    if (picked != null)
                    {
                        break;
                    }
                    //Log.Message("6");
                }
                //Log.Message("7");
            }

            //Log.Message("33333");
            if (picked != null)
            {
             
                if (kaczka != null)
                {
                    var mapa = pawn.Map.GetComponent<Classifier>();

                    if (pawn.Position.DistanceTo(picked.Position) <= kaczka.range)
                    {
                        if (GenSight.LineOfSightToThing(pawn.Position, picked, pawn.Map) && !mapa.wpiriot)
                        {
                            var attackJob = new Job(JobDefOf.AttackStatic, picked);

                            //Log.Message("hehehe");

                            mapa.wpiriot = true;

                            return attackJob;
                        }

                    }
                }
              
            }
            //Log.Message("33333333");

            return null;
        }
    }
}
