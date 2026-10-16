# Complemento da organização — 16/10/2026

O plano de 15/10 já estava aplicado. As revisões posteriores da Library, do Cylinder e da entrada passaram a reutilizar assets ainda localizados dentro de outros cenários. Esta rodada aplica a mesma regra do plano: recursos compartilhados ficam em Global.

## Mudanças

**94 assets centralizados com `AssetDatabase.MoveAsset`:**

| Tipo | Quantidade |
| --- | ---: |
| Texturas PNG | 52 |
| Ambiente HDR | 1 |
| Meshes persistentes | 19 |
| Materiais | 14 |
| Modelos FBX | 5 |
| Prefabs | 3 |

O destino preserva a procedência: `Assets/_Project/Global/<Tipo>/<CenárioDeOrigem>/<SubpastasOriginais>`. As sete pastas de cenários, a cena Main e as duas cenas de recuperação continuam nos locais definidos pelo plano.

Foram criadas 62 pastas de destino com conteúdo. Seis pastas que ficaram vazias foram arquivadas, com seus metas, em `_Quarantine/2026-10-16/EmptyFolders`. Nenhum asset foi excluído, fundido ou enviado à quarentena nesta rodada. A quarentena de 15/10 e os casos incertos continuam preservados.

O menu de medkit e as ferramentas permanentes continuam em `Assets/_Project/Editor/Tools`. Os arquivos em `Tools/ProjectOrganization/Backups` são cópias anteriores, fora da importação da Unity; não devem ser tratados como scripts ativos nem atualizados para os caminhos novos.

## Caminhos e preservação

- `ProjectAssetPaths` recebeu aliases para os 94 caminhos antigos, para consumidores de Editor que construam caminhos de texto.
- O resolvedor dos verificadores históricos agora acompanha os mapas de caminhos das rodadas datadas, sem sobrescrever o mapa de 15/10.
- Dois manifestos de procedência do Basement foram atualizados onde mencionavam recursos movidos.
- A ferramenta de auditoria aceita `-organizationOutput`, permitindo novos snapshots, journals e builds por rodada, sem substituir as evidências anteriores.
- Os conteúdos dos cenários, prefabs, materiais, meshes, modelos, texturas e importadores são preservados; apenas os consumidores textuais previstos recebem edições de caminhos.
- A SampleScene salva pelo usuário antes de fechar a Unity foi incluída na linha de base; seu patch de 202.718 bytes está em `user-saved-scene.patch`. Essa alteração não foi revertida nem reformada para remover espaços gerados pela serialização Unity.

## Validação desta rodada

A Unity 6000.5.7f1 abriu **7 cenas e 47 prefabs antes e depois**. Os **5.332 assets** dos snapshots mantiveram tipos, dependências recursivas por GUID, contagens de objetos/componentes, bindings de materiais/meshes e contagens de referências ausentes.

O cruzamento dos consumidores na Unity confirmou **zero assets compartilhados ainda localizados nas pastas de cenários**. A checagem inicial também não encontrou GUIDs duplicados, metas ausentes, conteúdo próprio fora da estrutura aprovada ou arquivos perdidos do mapa anterior.

O resultado da comparação de **11.037 arquivos da linha de base**, incluindo metas e configurações, está em [validation.json](validation.json). Os hashes novos permitidos ficam em [edit-journal.json](edit-journal.json). [build-result.json](build-result.json) registra o resultado do Development Build Windows desta rodada. Logs completos: `.utmp/ProjectOrganization/2026-10-16/`.

**Build Windows aprovado: zero erros e cinco avisos.** Executável: `.utmp/ProjectOrganization/2026-10-16/Build/UniDoom.exe`. Os três arquivos de configuração regravados pelos hooks de build foram devolvidos aos bytes da linha de base; a lista está em [build-incidental.json](build-incidental.json). Essas gravações incidentais não integram a mudança final.

Os problemas preexistentes foram registrados separadamente em [preexisting-problems.json](preexisting-problems.json): zero scripts ausentes, três referências ausentes e 24 MeshFilters vazios somados entre cenas e prefabs auditados. Essas contagens permaneceram iguais depois da movimentação; não foram corrigidas por alterações de gameplay nesta tarefa.

Não foram executados testes novos de Play Mode, comparação visual por capturas, bake ou desempenho. Os registros anteriores não substituem testes da versão atual. A reversão foi testada em fixture isolada para recuperar arquivos, metas, pastas e consumidores, preservar pastas novas na quarentena e recusar alterações posteriores.

`git diff --check` passou para os arquivos desta tarefa. A SampleScene salva pelo usuário foi excluída apenas dessa consulta de espaços em branco, pois a serialização contém campos vazios com espaço final; os bytes da cena foram preservados na comparação de integridade.

## Registros e reversão

- [Plano complementar](PLAN.md), [arquivos movidos](moved-files.csv), [mapa de caminhos](path-map.json) e [journal Unity](move-journal.json).
- [Pastas novas](created-folders.json), [pastas vazias arquivadas](empty-folders.json) e [linha de base](baseline.json).
- Snapshots `unity-before.json` e `unity-after.json`; relatório `validation.json`.
- Backups dos consumidores e das configurações em `Backups`, fora de caches temporários.

Para verificar ou desfazer **somente esta rodada**, a partir da raiz:

```powershell
py Tools/ProjectOrganization/2026-10-16/rollback.py --check
# Feche a Unity antes de aplicar a reversão:
py Tools/ProjectOrganization/2026-10-16/rollback.py --apply
```

A checagem bloqueia a sobrescrita de modificações posteriores. A aplicação exige a Unity fechada, restaura cada arquivo junto do meta original e guarda as pastas novas vazias fora de Assets. Não há exclusão permanente. As versões da ferramenta de auditoria anterior à adaptação também estão guardadas em Backups; sua adaptação reutilizável e os relatórios permanecem disponíveis.

O rollback de 15/10 não deve ser usado para apagar as revisões posteriores do jogo. Seus bloqueios de hashes são intencionais. O novo rollback atua exclusivamente sobre esta centralização de 16/10.

Para repetir a verificação estática com os snapshots salvos:

```powershell
py Tools/ProjectOrganization/2026-10-16/continue_organization.py verify
```

Esse comando confere os arquivos atuais e os snapshots produzidos na Unity; ele não inicia outra sessão do Editor.
