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

namespace YO
{
    public class EscorterExt : DefModExtension
    {
        public int Priority;

        public int maxCount;

        public bool RequesterProvider;
    }

    public class IsBreacher : DefModExtension
    {
    }


    [DefOf]
    public class BallsDefOf : DefOf
    {
        public static PawnKindDef Mech_CentipedeGunner;

        public static PawnKindDef Mech_Scyther;

        public static DutyDef StormColony;
    }

    public class WaitForSignalThenAssault : LordJob
    {
        public WaitForSignalThenAssault()
        {

        }

        public IntVec3 location;

        public WaitForSignalThenAssault(Faction fact, IntVec3 pos)
        {
            location = pos;
        }

       

        public override StateGraph CreateGraph()
        {
            var graph = new StateGraph();

            var toil1 = new LordToil_Stage(location);

            graph.AddToil(toil1);

            var toil2 = new LordToil_AssaultColony();

            var trans = new Transition(toil1, toil2);

            graph.StartingToil = toil1;

            trans.AddTrigger(new Trigger_PawnsLost(3));

            trans.AddTrigger(new Trigger_TicksPassed(12000));

            trans.AddTrigger(new Trigger_Custom(x => x.Pawn?.Map?.GetComponent<Classifier>()?.wpiriot ?? false));

            graph.AddToil(toil2);

            graph.AddTransition(trans);

            return graph;
        }
    }

    public class EscortThenAsalt : LordJob_EscortPawn
    {
        public EscortThenAsalt(Pawn escortee, Thing shuttle)
        {
            this.escortee = escortee;
            this.shuttle = shuttle;
        }

        public override StateGraph CreateGraph()
        {
            var graph = new StateGraph();

            var toil1 = new LordToil_EscortPawn(this.escortee, 3);

            graph.AddToil(toil1);

            var toil2 = new LordToil_AssaultColony();

            var trabs = new Transition(toil1, toil2);

            trabs.AddTrigger(new Trigger_PawnLost(PawnLostCondition.Undefined, escortee));

            graph.AddToil(toil2);

            graph.AddTransition(trabs);

            return base.CreateGraph();
        }
    }


    public class Shturm : RaidStrategyWorker_StageThenAttack
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            return base.MakeLordJob(parms, map, pawns, raidSeed);
        }

        public int centiC;


        public override void MakeLords(IncidentParms parms, List<Pawn> pawns)
        {
            var map = parms.target as Map;

            var mapcop = map.GetComponent<Classifier>();

            mapcop.FindDefensiveLines();

            mapcop.wpiriot = false;

            var loc = RCellFinder.FindSiegePositionFrom(parms.spawnCenter.IsValid ? parms.spawnCenter : pawns[0].PositionHeld, map);

            if (mapcop.lines.NullOrEmpty())
            {
                //Log.Message("shit");
            }

            ////Log.Message("1999");
            foreach (var b in mapcop.lines)
            {
                ////Log.Message(b.stuff.Where(x => x?.def?.defName?.ToLower().Contains("embr") ?? false).FirstOrDefault()?.Position.ToString() ?? "2222222222"); ;
            }

            List<Pawn> escorters = new List<Pawn>();
            List<Pawn> tanks = new List<Pawn>();
            List<Pawn> normies = new List<Pawn>();
            foreach (var p in pawns)
            {
                var ext = p.kindDef.GetModExtension<EscorterExt>();
                if (ext != null)
                {
                    if (ext.RequesterProvider)
                    {
                        tanks.Add(p);
                    }
                    else
                    {
                        escorters.Add(p);
                    }
                }
                else
                {
                    normies.Add(p);
                }
            }

            if (tanks.Any())
            {
                foreach (var asalt in tanks.OrderBy(x => x.kindDef.GetModExtension<EscorterExt>().Priority))
                {
                    //Log.Message(asalt.kindDef.GetModExtension<EscorterExt>().maxCount.ToString());
                    var doodies = escorters.GetRange(0, Mathf.Min(escorters.Count, asalt.kindDef.GetModExtension<EscorterExt>().maxCount));
                    foreach (var dudes in doodies)
                    {
                        ////Log.Message(dudes.Label.Colorize(Color.blue));
                    }
                    escorters.RemoveAll(x => doodies.Contains(x));
                    var lordJob2 = new LordJob_EscortPawn(asalt, null);
                    var lord2 = LordMaker.MakeNewLord(parms.faction, lordJob2, map, doodies);
                    lord2.inSignalLeave = parms.inSignalEnd;
                    QuestUtility.AddQuestTag(lord2, parms.questTag);

                    if (escorters.NullOrEmpty())
                    {
                        break;
                    }
                }
            }
            else
            {
                ////Log.Message("ze co kurwa");
            }

            List<Pawn> paln = new List<Pawn>();

            foreach (var p in pawns.Except(escorters))
            {
                if (p.kindDef.HasModExtension<IsBreacher>())
                {
                    paln.Add(p);
                    ////Log.Message(p.kindDef.label + " " + p.Label);
                }
            }

            if (paln.Any())
            {
                normies.AddRange(tanks);
                normies.AddRange(escorters);
                normies.RemoveAll(x => paln.Contains(x));

                var kingJob = new Szturm(/*parms.faction*/);
                var king = LordMaker.MakeNewLord(parms.faction, kingJob, map, paln);
                king.inSignalLeave = parms.inSignalEnd;
                QuestUtility.AddQuestTag(king, parms.questTag);

                var lordJob = new WaitForSignalThenAssault(parms.faction, loc);
                var lord = LordMaker.MakeNewLord(parms.faction, lordJob, map, normies);
                lord.inSignalLeave = parms.inSignalEnd;
                QuestUtility.AddQuestTag(lord, parms.questTag);
            }
            else
            {
                normies.AddRange(tanks);
                normies.AddRange(escorters);

                var kingJob = new LordJob_AssaultColony(/*parms.faction*/);
                var king = LordMaker.MakeNewLord(parms.faction, kingJob, map, paln);
                king.inSignalLeave = parms.inSignalEnd;
                QuestUtility.AddQuestTag(king, parms.questTag);

                var lordJob = new LordJob_AssaultColony(parms.faction);
                var lord = LordMaker.MakeNewLord(parms.faction, lordJob, map, normies);
                lord.inSignalLeave = parms.inSignalEnd;
                QuestUtility.AddQuestTag(lord, parms.questTag);
            }


            //base.MakeLords(parms, pawns);
        }
    }

    public class MechTactic : RaidStrategyWorker_ImmediateAttack
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            return base.MakeLordJob(parms, map, pawns, raidSeed);
        }

        public int centiC;
        #region bad
        /* public override bool CanUsePawn(float pointsTotal, Pawn p, List<Pawn> otherPawns)
         {
             if (otherPawns.NullOrEmpty())
             {
                 Debug.//Log(p.def.defName);
                 return p.def.defName.ToLower().Contains("centipede");
             }
             else
             {
                 var centis = otherPawns.FindAll(x => x.def.defName.ToLower().Contains("centipede")).Count();
                 //Log.Message(centis.ToString());
                 var scythers = otherPawns.FindAll(x => x.def.defName.ToLower().Contains("scyther")).Count();

                 if (centis * 2 <= scythers)
                 {
                     //Log.Message("1");
                     //var pr2 = new PawnGenerationRequest(BallsDefOf.Mech_Scyther, p.Faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, biocodeWeaponChance: 0f, biocodeApparelChance: 0f, allowFood: false);
                     //var p2 = PawnGenerator.GeneratePawn(pr2);
                     //p = p2;
                     return base.CanUsePawn(pointsTotal, p, otherPawns);
                 }
                 else
                 {
                     //Log.Message("2");
                     return p.def.defName.ToLower().Contains("scyther");
                 }
             }
             //return base.CanUsePawn(pointsTotal, p, otherPawns);
         }*/



        /*public override List<Pawn> SpawnThreats(IncidentParms parms)
        {
            //Log.Message("kupa");
            if (parms.pawnKind != nulltrue)
            {
                //Log.Message("kupa2");
                List<Pawn> list = new List<Pawn>();
                float flomt = parms.points;

                while(flomt > 0)
                {
                    //Log.Message("12345w");
                    var pr = new PawnGenerationRequest(BallsDefOf.Mech_CentipedeGunner, parms.faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, biocodeWeaponChance: parms.biocodeWeaponsChance, biocodeApparelChance: parms.biocodeApparelChance, allowFood: def.pawnsCanBringFood);
                    var p = PawnGenerator.GeneratePawn(pr);
                    list.Add(p);
                    flomt -= BallsDefOf.Mech_CentipedeGunner.combatPower;

                    //Log.Message(Mathf.CeilToInt(flomt / BallsDefOf.Mech_Scyther.combatPower).ToString());
                    for (int i = Mathf.CeilToInt(flomt / BallsDefOf.Mech_Scyther.combatPower); i >= 0; i--)
                    {
                        var pr2 = new PawnGenerationRequest(BallsDefOf.Mech_Scyther, parms.faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, biocodeWeaponChance: parms.biocodeWeaponsChance, biocodeApparelChance: parms.biocodeApparelChance, allowFood: def.pawnsCanBringFood);
                        var p2 = PawnGenerator.GeneratePawn(pr2);
                        list.Add(p2);
                        flomt -= BallsDefOf.Mech_Scyther.combatPower;
                    }
                }

                return list;
            }
            return null;
        }*/
        #endregion


        public override void MakeLords(IncidentParms parms, List<Pawn> pawns)
        {
            #region bad
            /*foreach (var p in pawns)
            {
                p.Destroy(DestroyMode.Vanish);
            }
            pawns.Clear();

            //Log.Message("kupa2");
            List<Pawn> list = new List<Pawn>();
            float flomt = parms.points;

            while (flomt > 0)
            {
                //Log.Message("12345w");
                var pr = new PawnGenerationRequest(BallsDefOf.Mech_CentipedeGunner, parms.faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, biocodeWeaponChance: parms.biocodeWeaponsChance, biocodeApparelChance: parms.biocodeApparelChance, allowFood: def.pawnsCanBringFood);
                var p = PawnGenerator.GeneratePawn(pr);
                list.Add(p);
                flomt -= BallsDefOf.Mech_CentipedeGunner.combatPower;

                //Log.Message(Mathf.CeilToInt(flomt / BallsDefOf.Mech_Scyther.combatPower).ToString());
                for (int i = Mathf.CeilToInt(flomt / BallsDefOf.Mech_Scyther.combatPower); i >= 0; i--)
                {
                    var pr2 = new PawnGenerationRequest(BallsDefOf.Mech_Scyther, parms.faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, biocodeWeaponChance: parms.biocodeWeaponsChance, biocodeApparelChance: parms.biocodeApparelChance, allowFood: def.pawnsCanBringFood);
                    var p2 = PawnGenerator.GeneratePawn(pr2);
                    list.Add(p2);
                    flomt -= BallsDefOf.Mech_Scyther.combatPower;
                }
            }
            

            pawns = list;*/
            #endregion

           

            var map = parms.target as Map;

            var mapcop = map.GetComponent<Classifier>();

            mapcop.FindDefensiveLines();

            if (mapcop.lines.NullOrEmpty())
            {
                //Log.Message("shit");
            }

            //Log.Message("1999");
            foreach (var b in mapcop.lines)
            {
                //Log.Message(b.stuff.Where(x => x?.def?.defName?.ToLower().Contains("embr") ?? false).FirstOrDefault()?.Position.ToString() ?? "2222222222"); ;
            }

            List <Pawn> escorters = new List<Pawn>();
            List<Pawn> tanks = new List<Pawn>();
            List<Pawn> normies = new List<Pawn>();
            foreach (var p in pawns)
            {
                ////Log.Message(p.def.defName.ToLower());
                var ext = p.kindDef.GetModExtension<EscorterExt>();
                if (ext != null)
                {
                    if (ext.RequesterProvider)
                    {
                        ////Log.Error("pipipupu");
                        ////Log.Message(p.kindDef.label + "2");
                        tanks.Add(p);
                    }
                    else
                    {
                        ////Log.Error("dwadwaddpipipupu");
                        ////Log.Message(p.kindDef.label + "3");
                        escorters.Add(p);
                    }
                }
                else
                {
                    ////Log.Error("222222222222222");
                    ////Log.Message(p.kindDef.label + "1");
                    normies.Add(p);
                }
            }

            if (tanks.Any())
            {
                foreach (var asalt in tanks.OrderBy(x => x.kindDef.GetModExtension<EscorterExt>().Priority))
                {
                    //Log.Message(asalt.kindDef.GetModExtension<EscorterExt>().maxCount.ToString());
                    var doodies = escorters.GetRange(0, Mathf.Min( escorters.Count, asalt.kindDef.GetModExtension<EscorterExt>().maxCount ) );
                    foreach (var dudes in doodies)
                    {
                        //Log.Message(dudes.Label.Colorize(Color.blue));
                    }
                    escorters.RemoveAll(x => doodies.Contains(x));
                    var lordJob2 = new LordJob_EscortPawn(asalt, null);
                    var lord2 = LordMaker.MakeNewLord(parms.faction, lordJob2, map, doodies);
                    lord2.inSignalLeave = parms.inSignalEnd;
                    QuestUtility.AddQuestTag(lord2, parms.questTag);

                    if (escorters.NullOrEmpty())
                    {   
                        break;
                    }
                }

                /*
                var lordJob1 = new LordJob_AssaultColony(Faction.OfMechanoids, false, false, false, true, false, false, false);
                var lord1 = LordMaker.MakeNewLord(Faction.OfMechanoids, lordJob1, map, tanks);
               
                lord1.inSignalLeave = parms.inSignalEnd;
                QuestUtility.AddQuestTag(lord1, parms.questTag);

                if (escorters.Any())
                {
                    var lordJob2 = new EscortThenAsalt(tanks[0], null);
                    var lord2 = LordMaker.MakeNewLord(Faction.OfMechanoids, lordJob2, map, escorters);
                    lord2.inSignalLeave = parms.inSignalEnd;
                    QuestUtility.AddQuestTag(lord2, parms.questTag);
                }*/
            }
            else
            {
                //Log.Message("ze co kurwa");
            }

            normies.AddRange(tanks);
            normies.AddRange(escorters);
            var lordJob = new LordJob_AssaultColony(parms.faction, false, false, false, true, false, false, false);
            var lord = LordMaker.MakeNewLord(Faction.OfMechanoids, lordJob, map, normies);
            lord.inSignalLeave = parms.inSignalEnd;
            QuestUtility.AddQuestTag(lord, parms.questTag);
            //base.MakeLords(parms, pawns);
        }
    }

    public class ModifiedAssault : LordToil_AssaultColony
    {
        public override void UpdateAllDuties()
        {
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                pawn.mindState.duty = new PawnDuty(BallsDefOf.StormColony);
            }
        }
    }

    public class Szturm : LordJob_AssaultColony
    {
        public override StateGraph CreateGraph()
        {
            var graf = new StateGraph();
            graf.AddToil(new ModifiedAssault());
            return graf;
        }
    }
}
