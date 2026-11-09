import pathlib,subprocess,hashlib,json
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
editable={'Assets/_Project/Global/Scripts/UI/GameMenus.cs','Assets/_Project/Global/Scripts/UI/GameMenus.Credits.cs','Assets/_Project/Global/Scripts/UI/CreditsSettings.cs','Docs/Credits.md','Docs/Maps/correcoesbloco4.md'}
paths=[p for p in subprocess.check_output(['git','diff','--name-only','-z']).decode().split('\0') if p and p not in editable]
paths+=['Assets/_Project/Global/Resources/Menus/Credits.asset','Assets/_Project/Global/Resources/Menus/Credits.asset.meta','Assets/_Project/Global/Resources/Menus/CreditsContent.txt','Docs/Maps/creditos.md']
paths += [p+'.meta' for p in editable if p.endswith('.cs')]
pathlib.Path('.local/CinemaCredits/baseline.json').write_text(json.dumps({p:sha(pathlib.Path(p)) for p in paths}),encoding='utf-8')
print('Protected existing map/weapon/vehicle edits, credits settings/content and script GUIDs.')
