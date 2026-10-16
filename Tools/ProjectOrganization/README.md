# Organização do projeto Unity — 15/10/2026

> Continuação em 16/10/2026: [centralização de 94 recursos que passaram a ser compartilhados pelas revisões posteriores](2026-10-16/README.md). Consulte esse relatório para o estado atual; os números e snapshots abaixo registram a rodada original.

O [plano revisado pelo usuário](PLAN.md) foi executado. Os cenários estão reunidos em `Assets/_Project/Scenes`, os recursos compartilhados e de sistemas em `Assets/_Project/Global`, e as ferramentas mantidas em `Assets/_Project/Editor/Tools`.

## Resultado

| Item | Resultado |
| --- | ---: |
| Operações internas com `AssetDatabase.MoveAsset` | 1.452 |
| Arquivos relocados dentro de Assets, incluindo a nova ferramenta de auditoria | 3.559 |
| Recursos organizados em Global | 176 |
| Texturas com consumidores em vários ambientes, centralizadas | 28 |
| Pastas novas, com novos GUIDs de pasta | 66 |
| Scripts geradores arquivados | 4 |
| Meshes redundantes arquivadas | 30 |
| Pastas antigas vazias arquivadas com seus metas | 32 |
| Arquivos preservados para revisão manual | 960 |

Os 34 arquivos em quarentena somam 460.277 bytes, sem contar metas e pastas. Não houve exclusão definitiva nem commit automático. A redução de espaço é pequena: esta tarefa priorizou organização e preservação, sem deduplicar referências ou descartar conteúdo incerto.

## Estrutura final

```text
Assets/
  _Project/
    Scenes/
      Main/                 SampleScene.unity e seu profile
      Basement/             arte, prefabs, scripts e preview do porão
      ClassRoom/            arte, prefabs e preview da sala
      Cylinder/             arte, meshes e preview do cilindro
      DirectorRoom/         arte, prefabs e preview da diretoria
      EntranceGarden/       arte, prefabs e script de layout do jardim
      EntranceHallway/      prefab do corredor
      Library/              prefab da biblioteca
      _Recovery/            as duas cenas de recuperação preservadas
    Global/
      Audio/ Fonts/ Materials/ Prefabs/ Profiles/
      Scripts/ Settings/ Shaders/ Sprites/ Textures/
    Editor/Tools/
  TextMesh Pro/
  ProBuilder Data/
  TutorialInfo/
  Unity.VisualScripting.Generated/
```

`Readme.asset` do template continua na raiz de Assets. Não foram criadas pastas vazias de Meshes, Terrain ou outros tipos sem conteúdo. As subpastas de etapas de arte foram mantidas onde preservam procedência, nomes e caminhos relativos de modelos. Os ambientes principais são sete prefabs de cenário; não foram inventadas novas cenas para ambientes que só possuíam prefab.

O prefab do porão agora fica em `Assets/_Project/Scenes/Basement/Prefabs/Basement.prefab`. O antigo `Assets/Art/Basement/BasementFinalArena.prefab` fica em `Assets/_Project/Scenes/Basement/Prefabs/BasementFinalArena.prefab`.

## Registros de movimentação e classificação

- [Arquivos movidos, incluindo quarentena](moved-files.csv): caminho original, destino e classificação para cada arquivo.
- [Todas as classificações](classification.csv) e [lista de revisão manual](manual-review.csv).
- [Pastas novas](created-folders.json), [journal das movimentações Unity](move-journal.json) e [mapa individual dos caminhos](path-map.json).
- [Arquivos enviados à quarentena](quarantine-journal.json) e [pastas vazias arquivadas](empty-folder-journal.json): original, destino, data, motivo e verificações.
- [Inventário original](audit-before.json), [metas originais](meta-before.json), [snapshot Unity antes](unity-before.json) e [depois](unity-after.json).

A classificação de revisão manual tem precedência sobre a classificação de localização: um arquivo incerto pode acompanhar a pasta do ambiente ou de Global, mas permanece preservado. São 699 arquivos `.asset`, 122 materiais, 90 PNGs, 33 JSONs, seis cenas avulsas/de recuperação, seis áudios, três prefabs e uma fonte. A ausência de consumidores serializados não foi tratada como prova de descarte.

As 39 famílias de arquivos com conteúdo bruto duplicado foram registradas no inventário. Texturas não foram fundidas: nomes construídos pelo conversor e configurações de importação distintas exigem preservação. Materiais originais e JSONs de procedência/reversão foram mantidos.

## Scripts e consumidores atualizados

Foram arquivados `TCMapBuilder`, `TCAssets`, `TCTheme` e `TCAtmosphere`, conforme escolha explícita do usuário. São scripts de Editor sem consumidores externos nos scripts mantidos, cenas, prefabs ou configurações auditados. Os resultados persistentes foram mantidos; as fontes históricas também existem em `Tools/TotalChaosSkill/Source`.

As 30 meshes arquivadas não têm referências de entrada no grafo da Unity, GUIDs ou caminhos literais ativos auditados. Cada uma possui uma mesh mantida com todos os campos serializados idênticos, exceto o nome. O journal identifica a correspondente; nenhum consumidor precisou ser remapeado.

`BasementArenaEncounter`, `BasementGateLayout` e `EntranceGardenLayout` continuam ativos nos ambientes: ainda controlam encounter ou ajustam o layout. Os quatro scripts runtime Total Chaos continuam em Global. `MedkitPrefabCreator` permanece no Editor porque não foi comprovado que sua função estivesse concluída; sua saída passa a usar `Global/Prefabs/Pickups` quando a ferramenta for executada.

A configuração de build aponta para a nova SampleScene. Os caminhos do conversor, das auditorias Basement/ClassRoom, das associações de textura e dos JSONs de procedência foram adaptados. `ProjectAssetPaths` resolve os 28 caminhos de texturas compartilhadas construídos pelas ferramentas. Todos os destinos foram conferidos. Includes relativos do shader e arquivos de modelos/importadores foram preservados.

Não há pacote Addressables, chamadas de carregamento Resources/AssetBundle ou StreamingAssets de autoria do projeto que exigissem migração. Os Resources de TextMesh Pro permanecem no local original. Shader.Find continua usando os nomes originais dos shaders. Pastas e pacotes externos não foram reorganizados.

Ferramentas de geração antigas em `Tools/` permanecem como fontes históricas, fora de Assets. Elas podem conter caminhos do layout anterior e precisam ser adaptadas ao mapa antes de serem reexecutadas. Os dois verificadores de otimização mantidos recebem resolução dos caminhos históricos, inclusive para meshes guardadas em quarentena.

## Validação executada

Unity **6000.5.7f1**, URP **17.5.0**, no computador local:

- Abertura real das **7 cenas e 31 prefabs**, antes e depois.
- Comparação de **3.603 assets** comuns aos snapshots Unity, sem diferenças de tipo, dependências recursivas por GUID, objetos/componentes, bindings serializados, scripts ausentes, referências ausentes, materiais ou meshes vazias.
- **3.636 arquivos originais** presentes e com hashes esperados. Apenas as edições textuais previstas usam hashes novos registrados em `edit-plan.json`; cenas, prefabs, materiais, meshes, texturas, áudio e shaders mantiveram os conteúdos da linha de base.
- **3.786 metas originais** com bytes idênticos, incluindo os arquivos e pastas preservados na quarentena. GUIDs e configurações de importação mantidos.
- **43 alterações de materiais salvas pelo usuário** antes da reorganização preservadas integralmente. Patch em `saved-before-organization.patch`; cópia adicional local em `.utmp/ProjectOrganization/saved-before`.
- Compilação do Editor e **Development Build Windows concluídos: zero erros, cinco avisos**, referentes a APIs obsoletas já presentes nos scripts. O log também contém avisos preexistentes do pacote Cinemachine sobre uma asmref de exemplo sem assembly alvo.
- Executável: `.utmp/ProjectOrganization/Build/UniDoom.exe`. Resultado em [build-result.json](build-result.json); log completo em `.utmp/ProjectOrganization/validation.log`.
- A Unity escreveu campos incidentais de prefilter/runtime settings do URP, batching do Player e uma preferência ProBuilder durante o build. Seus bytes anteriores foram restaurados, e a verificação de conteúdo passou depois dessa restauração. As versões incidentais estão em `.utmp/ProjectOrganization/build-incidental`.
- A reversão passou em fixture isolada para movimentos de arquivo/pasta e metas, recuperação de quarentena e bloqueio de modificações posteriores; o estado real final também passou em `rollback.py --check`.
- `git diff --check` passou com os filtros LFS desabilitados apenas nessa consulta; os filtros foram desabilitados para evitar a escrita de temporários em `.git` durante uma verificação de leitura.

Resultado detalhado: [validation.json](validation.json). Para repetir a comparação estática com os snapshots preservados: `py Tools/ProjectOrganization/verify_organization.py`. Isso não reabre a Unity; os snapshots foram produzidos pelas execuções reais registradas nos logs.

Não foram executados novos testes de Play Mode, comparação visual por capturas, bake de iluminação, desempenho ou hardware-alvo. Preservação de conteúdo e referências não substitui esses testes.

## Problemas preexistentes e limites

Continuam presentes, com as mesmas contagens da linha de base:

- Uma referência ausente em cada uma das duas cenas de recuperação e uma em `Global/Prefabs/Weapons/Gun.prefab`.
- MeshFilters sem mesh em objetos de SampleScene, recuperação, ClassRoom, DirectorRoom e prefabs de portas. A lista exata está em `validation.json`; nenhuma atribuição foi alterada para mascarar esses casos.
- Avisos de APIs obsoletas e do pacote Cinemachine descritos acima.

O verificador histórico ClassRoom passou após a adaptação dos caminhos; suas imagens são capturas de uma tarefa anterior, não capturas novas desta organização. O verificador histórico Basement falha porque seu backup antigo tem **2.945 fileIDs**, enquanto o prefab já tinha **2.991 fileIDs no HEAD e na linha de base desta tarefa**. O conteúdo atual desse prefab tem exatamente o hash anterior à organização; essa divergência de backup não foi causada pelas movimentações. Não substituímos o backup histórico nem relaxamos a asserção.

Não foram detectadas novas falhas de integridade ou referências. Os 960 casos de revisão manual e os problemas antigos acima permanecem documentados, sem descarte ou mudanças de gameplay.

## Reversão

Feche a Unity e execute a partir da raiz:

```powershell
py Tools/ProjectOrganization/rollback.py --check
py Tools/ProjectOrganization/rollback.py --apply
```

`--check` confere hashes, disponibilidade e integridade dos backups e arquivos novos, sem modificar assets; pode ser executado com o Editor aberto. `--apply` também exige que o lock da Unity esteja livre. Se houver alterações posteriores, recusa a reversão; não use reset ou sobrescrita para contornar isso. Depois de uma edição sua, será necessário revisar a reversão seletivamente.

`--apply` restaura os caminhos originais, as edições textuais anteriores e os arquivos de quarentena com seus metas. Assets auxiliares novos e pastas novas vazias são preservados em `RollbackArtifacts`/`RollbackFolders` dentro da quarentena. A movimentação de reversão ocorre com a Unity fechada, mantendo os pares arquivo/meta; reabra o Editor para reimportar. Relatórios e regras de organização permanecem disponíveis.

Os backups necessários das edições estão em **`Tools/ProjectOrganization/Backups`**, fora de caches temporários. Preserve-os junto dos journals, `state-after.json` e da quarentena. O script só aceita a reversão completa do estado exato registrado; para recuperar um único arquivo, use o mapa e preserve seu meta.

## Próximas gerações

As regras permanentes estão em `AGENTS.md`: cenário próprio, assets exclusivos locais, compartilhados em Global, reutilização de assets existentes, resultados persistentes, ferramentas temporárias identificadas e descarte somente após validação. A quarentena continua exigindo autorização explícita para qualquer exclusão definitiva.
