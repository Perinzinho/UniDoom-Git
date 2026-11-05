"""Build the requested review artifacts from the recorded static and Editor evidence."""
import collections
import hashlib
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT/'.ai/map-review'

def read(name):
    return json.loads((OUT/name).read_text(encoding='utf-8'))

def write(name, value):
    path = OUT/name
    temporary = path.with_suffix(path.suffix+'.tmp')
    temporary.write_text(value if isinstance(value,str) else json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
    temporary.replace(path)

def main():
    audit = read('static-audit.json')
    engine = read('unity-validation.json')
    baseline = read('baseline-hashes.json')
    plan = read('editor-move-plan.json')
    changes = read('applied-changes.json')
    assert engine['status'] == 'completed' and not engine['compilationFailed']
    assets = {a['path']:a for a in audit['assets']}
    fixed_guids = {r['old_guid'] for r in plan['replacements']+plan['restores']}
    repaired_materials = set()
    # A restored original GUID fixes its consumers without rewriting their files.
    for name in assets:
        if name.endswith('.mat') and name.startswith('Assets/'):
            original = __import__('subprocess').run(['git','show','HEAD:'+name],capture_output=True)
            if original.returncode == 0 and any(g.encode() in original.stdout for g in fixed_guids):
                repaired_materials.add(name)
    by_scene = {s['path']:s for s in engine['scenes']}
    labels = {
        'Basement': 'Serviço subterrâneo, santuário, arena e prisão; umidade localizada e luzes de trabalho.',
        'ClassRoom': 'Sala de aula institucional abandonada; leitura de mesas, quadro e passagem.',
        'Cylinder': 'Circulação institucional e hub cilíndrico; preservar portas, acessos e orientação.',
        'EntranceGarden': 'Jardim e recepção noturnos; contraste entre exterior e passagem institucional.',
        'Library': 'Biblioteca universitária abandonada; estantes, circulação e encontro com inimigos.',
        'SampleScene': 'Cena integrada dos setores; sala do diretor embutida excluída da inspeção no Editor.'
    }
    scenes=[]
    for original in audit['scenes']:
        entry=dict(original)
        assert hashlib.sha256((ROOT/entry['path']).read_bytes()).hexdigest()==baseline['scenes'][entry['path']]
        entry['before_sha256']=baseline['scenes'][entry['path']]
        if not entry['eligible']:
            scenes.append(entry)
            continue
        actual=by_scene[entry['path']]
        name=pathlib.Path(entry['path']).stem
        repaired=sorted(repaired_materials.intersection(entry['dependencies']))
        dependency_hashes={p:assets[p]['sha256'] for p in entry['dependencies'] if p in assets}
        importer_hashes={p:assets[p].get('meta_sha256') for p in entry['dependencies'] if p in assets}
        entry.update(status='blocked',audited=True,corrected=bool(repaired),technically_validated=True,
            full_validation='NOT_VERIFIED',rules_applied=['R01','R02','R03','R04','R05','R06','R07','R08','R09','R10'],
            repaired_materials=repaired,dependency_hashes=dependency_hashes,importer_hashes=importer_hashes,
            dependency_fingerprint=hashlib.sha256(json.dumps([dependency_hashes,importer_hashes],sort_keys=True).encode()).hexdigest(),
            environment_intent=labels[name],editor_result=actual,
            checks={
                'R01_references':'BLOCKED',
                'R02_preservation':'PASS',
                'R03_changed_asset_organization':'PASS' if repaired or name=='ClassRoom' else 'NOT_APPLICABLE',
                'R04_editor_load_and_scripts':'PASS',
                'R05_rendering_and_material_visual_quality':'NOT_VERIFIED',
                'R06_causal_wear_and_composition':'NOT_VERIFIED',
                'R07_lighting_and_legibility':'NOT_VERIFIED',
                'R08_navigation_and_gameplay':'NOT_VERIFIED',
                'R09_target_performance':'NOT_VERIFIED',
                'R10_editor_inspection':'PASS'},
            blocks=['Referência antiga em Global/Prefabs/Weapons/Gun.prefab; preservada para não afetar a sala do diretor.',
                'Sem comparação visual em câmera de jogo, Play Mode, build ou profiling no hardware-alvo.'],
            visual_score={'status':'NOT_VERIFIED','score':None,'maximum':24,'required_minimum':18,
                'reason':'Não atribuir pontuação de aparência ou desempenho sem observação e medição.'},
            last_result='Carregada e inspecionada na Unity; correções de textura verificadas estaticamente; aprovação integral pendente.')
        scenes.append(entry)
    assert len(repaired_materials)==21
    assert not any(i.get('guid') in fixed_guids for s in scenes for i in s.get('issues',[]))
    for name,digest in baseline['director'].items():
        assert hashlib.sha256((ROOT/name).read_bytes()).hexdigest()==digest
    manifest={'generated_utc':audit['generated_utc'],'project_root':'.',
        'inputs':{'SKILL_ORIGINAL':'.codex/SKILL.md','PROMPT_ORIGINAL':'.codex/PROMPT_MESTRE.md',
            'review_skill':'.codex/reviewer/SKILL.md','execution_prompt':'.codex/reviewer/PROMPT-DE-EXECUCAO.md',
            'MAP_GLOBS':['Assets/**/*.unity'],'EXCLUDE_GLOBS':['Assets/_Project/Scenes/DirectorRoom/**','**/DirectorRoom subtree in SampleScene'],
            'BATCH_SIZE':5,'MODE':'fix','REPORT_DIR':'.ai/map-review'},
        'rules_sha256':audit['rules'],'authorship':audit['authorship'],
        'counts':{'identified':7,'eligible':6,'excluded':1,'audited':6,'corrected':3,'technically_validated':6,
            'fully_validated':0,'blocked':6,'not_verified_full':6,'repaired_texture_references':56,'repaired_materials':21},
        'batches':[[s['path'] for s in scenes if s['eligible']][:5],[s['path'] for s in scenes if s['eligible']][5:]],
        'incremental_policy':'Reutilizar parsing apenas se hash do asset, importador, scanner e índice GUID não mudaram. Repercorrer dependências; aprovação visual depende também de regras, cena e fingerprint transitivo.',
        'scenes':scenes}
    write('map-manifest.json',manifest)
    write('rules-checklist.md', '''# Checklist normativa

Fontes: `.codex/reviewer/SKILL.md`, `.codex/reviewer/PROMPT-DE-EXECUCAO.md`, `.codex/SKILL.md` (direção visual), `.codex/PROMPT_MESTRE.md`, `AGENTS.md` e exclusão explícita da sala do diretor. Os campos do prompt foram resolvidos no manifesto, sem alterar os templates do usuário.

| Regra | Exigência verificável | Origem |
|---|---|---|
| R01 | GUID/fileID e dependências resolvidos; investigar referências faltantes sem substituição arbitrária | Reviewer / Checklist e Edição segura; mestre / regras 1 e 7 |
| R02 | Preservar cenas, prefabs, importadores, overrides, scripts, tags/layers e trabalho anterior | Mestre / regras 2 e 7; AGENTS |
| R03 | Reutilizar imagens existentes; recursos compartilhados em Global; mover com AssetDatabase.MoveAsset | AGENTS / Organização |
| R04 | Confirmar Unity/pipeline; abrir cenas, inspecionar scripts e shaders no Editor | Direção visual / Implementação; mestre / regras 1 e 8 |
| R05 | Materiais distinguíveis, PBR quando compatível, escala de textura coerente | Direção visual / Texturas e materiais |
| R06 | Arquitetura funcional, escala plausível, desgaste causal, composição e narrativa próprias | Direção visual / Princípios, Geometria, Desgaste e Composição |
| R07 | Fontes luminosas plausíveis, sombras navegáveis e efeitos moderados | Direção visual / Iluminação |
| R08 | Colisão, navegação, chegada por portas, encontros e interação preservados | Reviewer / Checklist; direção visual / Composição |
| R09 | Otimizar só com evidência; medir CPU/GPU em ms, memória, GC, SetPass e draw calls no alvo | Mestre / regras 1, 3, 6 e 10 |
| R10 | Comparar antes/depois e registrar testes reais; nunca equiparar auditoria estática à aprovação visual | Reviewer / Validar e Conclusão; mestre / regras 8 e 10 |

`PASS` exige evidência. `BLOCKED` indica impedimento concreto à conclusão/correção. `NOT_VERIFIED` indica verificação não executada. `NOT_APPLICABLE` não é aprovação visual.

O checklist visual original tem 12 critérios (escala, materiais, desgaste, luz, sombras/rota, profundidade, abandono, props, legibilidade, identidade, pós e desempenho). A pontuação permanece `null`, sem aprovação 18/24: inspeção serializada não permite atribuir notas de aparência ou desempenho. As seis cenas possuem estados individuais no manifesto.
''')
    rows=[]
    for s in scenes:
        if not s['eligible']:continue
        e=s['editor_result']
        rows.append(f"| {pathlib.Path(s['path']).stem} | {e['objects']} | {e['renderers']} | {e['lights']} / {e['realtimeShadowLights']} | {len(s['repaired_materials'])} | PASS técnico; pendente integral |")
    report='''# Revisão dos cenários — 20/10/2026

7 cenas identificadas; 6 elegíveis; 1 excluída (`DirectorRoom.unity`). 6 auditadas; 3 com referências de textura corrigidas; 6 carregadas e inspecionadas na Unity; 0 aprovadas integralmente; 6 com pendências de validação. Autoria por IA não confirmada: foram incluídas todas as cenas elegíveis, inclusive Basement fora da lista atual do build.

A exclusão inclui a subárvore `Environment/SampleScenario/DirectorRoom (2)`. Os 347 arquivos da pasta DirectorRoom e os sete arquivos .unity têm os mesmos SHA-256 iniciais. Nenhum prefab, script de gameplay, shader, perfil de luz ou configuração global foi alterado. O único conteúdo preexistente fora do Git era `.codex/reviewer/`, preservado.

## Diagnóstico e mudanças

Foram comprovadas 56 referências a 14 GUIDs antigos de textura em 21 materiais usados por renderizadores do porão (15 materiais), cilindro (6) e cena integrada (21, os mesmos recursos). O histórico Git mostra trocas de GUID anteriores à revisão. A identidade das imagens foi verificada pelo SHA-256 do objeto Git LFS, não apenas pelo nome.

- Nove referências lógicas de imagem reutilizam assets atuais idênticos; oito texturas foram movidas de ClassRoom/Lighting/Textures para `Assets/_Project/Global/Textures/MapSurfaces/` via AssetDatabase.MoveAsset, junto dos importadores originais. Paint_Albedo reutiliza o Metal_Albedo já global, com conteúdo idêntico.
- Cinco mapas necessários foram recuperados do LFS local para MapSurfaces: Organic_Albedo, Organic_Normal, Organic_MetalRough, Paint_Normal e Paint_MetalRough. Seus GUIDs e importadores históricos foram preservados. Paint_Normal tem pixels idênticos a um Metal_Normal existente, mas esse asset é importado como cor/sRGB, incompatível com normal map; preservou-se a importação de normal sem alterar o consumidor de cor.
- Os 17 materiais editados têm apenas 46 substituições de GUID. Outros quatro materiais voltam a resolver seus mapas pelos cinco assets recuperados. FileIDs, parâmetros de cor, roughness/smoothness, tiling, keywords e render states foram preservados. A aparência pretendida dos materiais passa a ter as texturas que faltavam; equivalência visual no jogo ainda não foi medida.
- Nenhuma pasta antiga Assets/Art, Assets/prefabs ou Assets/TotalChaos foi recriada. Os novos assets são dependências efetivas do jogo, não backups ou quarentena.

## Evidências por cena

Contagens do Editor incluem objetos inativos e prefabs expandidos. Luzes: total / realtime ativas com sombras. Não representam custo de um frame nem benchmark.

| Cena | Objetos | Renderizadores | Luzes | Materiais reparados | Resultado |
|---|---:|---:|---:|---:|---|
'''+ '\n'.join(rows)+'''

A direção de cada ambiente e os perfis de atmosfera estão registrados no manifesto. Há diferenciação serializada entre sala de aula, biblioteca, jardim noturno, cilindro e sub-regiões do porão; isso não comprova composição ou legibilidade final. Não foram inventadas notas visuais nem aplicados ajustes estéticos universais.

## Pendências e ranking

| Prioridade | Evidência / problema | Ação / risco / esforço | Como concluir |
|---|---|---|---|
| P0, resolvida | 56 referências de textura sem destino | Recuperação/reuso comprovado; baixo risco estrutural, aparência precisa de comparação | Checagem GUID concluída; comparar capturas em câmera de jogo |
| P1 | Gun.prefab mantém sprite `d73b5e995f0fe33449121aa9eb89aa33` ausente | BLOCKED nesta execução: recurso global também usado pela sala do diretor excluída; não tocar no global. WeaponSpriteAnimator.Start/SetGun atribui idleFrames válidos em runtime; impacto real ainda não testado | Revisar o fallback da UI em tarefa que inclua esse sistema; não substituir fileID por suposição |
| P1 | Aprovação visual e navegação não observadas em jogo | NOT_VERIFIED, todas as cenas; esforço de walkthrough e capturas | Verificar os 12 critérios, sem zero nos critérios essenciais, e mínimo 18/24 |
| P1 | Aviso preexistente de redução de resolução de sombras no atlas 2048² | Não aumentar atlas nem remover sombras sem avaliar VRAM/GPU; risco visual e de memória | Profiler + Frame Debugger e capturas; medir CPU/GPU ms no alvo |
| P2 | Material ProBuilderDefault com shader de erro e cinco GUIDs de pacote sem destino | NOT_APPLICABLE à renderização atual: seis renderizadores do corredor têm enabled=false, no jardim e na cena integrada. Pacote preservado | Só revisar se forem reativados; não modificar PackageCache |
| P2 | MeshFilters de portas sem mesh própria, com renderizadores filhos | NOT_VERIFIED como defeito: podem ser contêineres intencionais; componentes e colisores preservados | Comparar geometria e colisão no Play Mode antes de remover ou preencher |
| P2 | Biblioteca e cena integrada possuem hierarquias grandes | Hipótese de custo de CPU/render submission, sem medição; nenhuma combinação genérica de meshes ou alteração de batching | Medir ms, GC, SetPass, draw calls, memória e culling no build |

## Testes efetivamente executados

- Unity 6000.5.7f1, URP 17.5.0, pipeline PC_RPAsset. Ferramenta temporária compilada e executada no Editor fora de Play Mode; seis cenas abertas em OpenPreviewScene e fechadas sem salvar; cena/seleção de cenas abertas preservada. Resultado final: `completed`, `compilationFailed=false`, sem mensagens capturadas durante a inspeção e sem scripts ausentes. A ferramenta teve duas incompatibilidades de API na primeira compilação, corrigidas antes das execuções; esses erros temporários não são atribuídos ao projeto.
- Inspeção de referências serializadas, materiais/shaders e prefabs expandidos. Não foram detectados erros de shader nos materiais inspecionados; o caso ProBuilder desativado está documentado acima. Não equivale à compilação de todas as variantes de um build.
- Inventário recursivo por GUID e fileID; nenhum GUID duplicado; os 14 GUIDs de textura reparados não permanecem sem destino. Os únicos GUIDs sem resolução são o sprite legado global e cinco referências do material externo ProBuilder.
- Hashes das cenas e da pasta excluída comparados; bytes de imagens/importadores movidos comparados; arquivos recuperados comparados com LFS; diff dos materiais verificado como substituição exclusiva de GUID.
- Busca de consumidores em código: sem caminhos de texto ativos para as oito texturas movidas. Resources.Load de DoorReloadScreen usa arte/áudio de transição em Resources, preservados; não houve alteração de carregamento dinâmico, Addressables, AssetBundles ou lista de build.
- **Não executados:** Play Mode, travessia por portas, encontros/combate, build, dispositivo-alvo, Profiler/Frame Debugger, capturas comparativas, avaliação em janela/tela cheia/monitores ou pontuação visual. Não há ganho de FPS comprovado.

## Retomada, evidência e reversão

`map-manifest.json` contém hashes da cena/regras/dependências/importadores, estados por regra, resultados do Editor e lotes 5+1. `static-audit.json` é o índice/cache; `review_maps.py` verifica novamente hashes e reutiliza parsing compatível. Mudança de dependência relevante invalida a aprovação, que nunca é inferida apenas do cache.

`editor-move-plan.json`, `editor-moves-completed.json`, `reference-recovery.json` e `applied-changes.json` registram origem histórica, SHA-256, destinos e substituições exatas. `baseline-hashes.json` guarda somente hashes, não cópias de assets. `unity-validation.json` registra a última inspeção. `MapReviewEditor.cs` permanece fora de Assets como fonte reproduzível; a cópia temporária compilada é removida ao final.

Para reverter, aplicar inversamente as substituições de `applied-changes.json` e mover as oito texturas de volta pela API da Unity, com os mesmos .meta. Os cinco mapas recuperados só devem ser removidos após reverter os consumidores e verificar dependências; esta revisão não executa exclusões definitivas. Não houve commit automático.

Referências normativas visuais: `.codex/SKILL.md`, seções Princípios, Materiais, Iluminação, Implementação e Checklist. Páginas de referência consultadas: [Hotel Lobby](https://www.moddb.com/mods/total-chaos/images/hotel-lobby) e [Shader implementation](https://www.moddb.com/mods/total-chaos/images/shader-implementation). Nenhum asset do mod foi copiado; essas páginas não substituem avaliação do jogo.
'''
    write('review-report.md',report)
    print(json.dumps(manifest['counts'],ensure_ascii=False))

if __name__=='__main__':
    main()
