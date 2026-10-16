from pathlib import Path

OUT=Path(__file__).resolve().parent
PREVIOUS=OUT.parent/'EncounterRevision'
WORK=OUT.parents[2]/'.utmp/LibraryPreview'

def adapt(name,new):
    text=(PREVIOUS/name).read_text(encoding='utf-8')
    for a,b in [('LibraryEncounterVerifier','LibraryPressureVerifier'),('LibraryEncounterPlay','LibraryPressurePlay'),('EncounterCapture','PressureCapture'),('Evidence/Encounter','Evidence/Pressure'),('LIBRARY_ENCOUNTER','LIBRARY_PRESSURE')]:text=text.replace(a,b)
    (OUT/new).write_text(text,encoding='utf-8')
    return text

text=adapt('LibraryEncounterVerifier.cs','LibraryPressureVerifier.cs')
text=text.replace('Require(length>50','Require(length>65')
text=text.replace('Seven spawn markers','Sixteen spawn markers')
text=text.replace('foreach(var point in director.waves.SelectMany(w=>w.points))',
'''Require(director.waves.Sum(w=>w.points.Length)==16,"Expected sixteen configured spawns");
        Require(director.blackoutLights.Length==3&&director.blackoutTubes.Length==9&&director.blackoutDuration==8,"Invalid simple blackout configuration");
        Require(art.Find("Key retrieval encounter/Narrow catalogue chicanes").childCount==6,"Expected six physical choke groups");
        foreach(var point in director.waves.SelectMany(w=>w.points))''')
(OUT/'LibraryPressureVerifier.cs').write_text(text,encoding='utf-8')

text=adapt('EncounterCapture.cs','PressureCapture.cs')
text=text.replace('V(7.8f,1.72f,-12.8f)','V(7.0f,1.72f,-14.0f)')
(OUT/'PressureCapture.cs').write_text(text,encoding='utf-8')

text=adapt('LibraryEncounterPlay.cs','LibraryPressurePlay.cs')
text=text.replace('static bool pursued;','static bool pursued, sawBlackout, sawRestore;\n    static bool[] initialLightStates;\n    static double collectedAt;')
text=text.replace('static readonly Dictionary<Enemy,double> seen=', 'static readonly Dictionary<Enemy,double> deathTimes=new Dictionary<Enemy,double>();\n    static readonly Dictionary<Enemy,double> seen=')
text=text.replace('phase=0;seen.Clear();originalPositions.Clear();return;',
'''phase=0;seen.Clear();deathTimes.Clear();originalPositions.Clear();sawBlackout=sawRestore=false;collectedAt=0;
                initialLightStates=director.blackoutLights.Select(l=>l.enabled).ToArray();return;''')
text=text.replace('maximumDoorAngle=Mathf.Max',
'''if(!director.ObjectiveCollected&&director.BlackoutActive)throw new Exception("Blackout fired before collection");
            if(director.BlackoutActive)
            {
                if(collectedAt==0)collectedAt=now;
                if(now-collectedAt>.15)
                {
                    if(director.blackoutLights.Any(l=>l.enabled))throw new Exception("Blackout circuit light stayed on");
                    foreach(var tube in director.blackoutTubes)
                    {
                        var block=new MaterialPropertyBlock();tube.GetPropertyBlock(block);
                        if(block.GetColor("_EmissionColor").maxColorComponent>.001f)throw new Exception("Blackout tube stayed emissive");
                    }
                    sawBlackout=true;
                }
            }
            else if(sawBlackout)
            {
                if(!director.blackoutLights.Select(l=>l.enabled).SequenceEqual(initialLightStates))throw new Exception("Blackout did not restore circuit states");
                sawRestore=true;
            }
            maximumDoorAngle=Mathf.Max''')
text=text.replace('phase==5&&now-phaseStarted>2','phase==5&&now-phaseStarted>2&&director.PendingSpawns==0&&sawRestore')
text=text.replace('director.SpawnedCount!=7','director.SpawnedCount!=16')
text=text.replace('if(enemy.IsDead&&now-seen[enemy]>2.0)Object.Destroy(enemy.gameObject);',
'''if(enemy.IsDead)
                {
                    if(!deathTimes.ContainsKey(enemy))deathTimes[enemy]=now;
                    if(now-deathTimes[enemy]>.3&&enemy.GetComponentsInChildren<Collider>().Any(c=>c.enabled))throw new Exception("Dead enemy blocked a choke point");
                }''')
text=text.replace('// Exercise the existing damage/death path, then remove test corpses to isolate route validation.', '// Exercise damage and leave bodies in the room while traversing the narrow route.')
text=text.replace('Test enemies were removed after damage/death checks so corpses did not change the route.', 'Bodies remained in the room; their collision released after death, preventing permanent blocked passages.')
text=text.replace('if(!pursued)throw', 'if(!sawBlackout||!sawRestore)throw new Exception("Simple blackout was not exercised");\n                if(!pursued)throw')
text=text.replace('Three one-shot waves spawned exactly seven local zombie variants; the return wave required objective collection.',
'''Three one-shot waves spawned exactly sixteen local zombie variants; the return wave required objective collection. Three light circuits and nine tube emissions shut off for eight seconds, then restored their initial states.''')
(OUT/'LibraryPressurePlay.cs').write_text(text,encoding='utf-8')

for name in ['LibraryPressureBuilder.cs','LibraryPressureVerifier.cs','LibraryPressurePlay.cs','PressureCapture.cs']:
    (WORK/'Assets/_TemporaryTools/Editor'/name).write_bytes((OUT/name).read_bytes())
(WORK/'Assets/_Project/Scenes/Library/Scripts/EncounterRevision/LibraryEncounterDirector.cs').write_bytes((OUT/'Runtime/LibraryEncounterDirector.cs').read_bytes())
(WORK/'Assets/_Project/Scenes/Library/Scripts/EncounterRevision/LibraryZombieNavigator.cs').write_bytes((OUT/'Runtime/LibraryZombieNavigator.cs').read_bytes())
print('PRESSURE_TOOLS_READY')
