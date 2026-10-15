# Basement: ritual, preparação e passagem

Direção visual: um porão industrial ocupado por um culto, com preparação de
oferendas na pré-arena, marcas de passagem nos corredores e um círculo de
sacrifício dominante na sala final. Paleta de ferro oxidado, cera amarelada,
tecido queimado, sangue seco e inscrições de osso/giz. Luz quente localizada
nas velas; iluminação industrial fria preserva o volume do ambiente.

A referência a [Hexatombe, de Ordem Paranormal](https://ordemparanormal.com.br/)
é uma interpretação de atmosfera: cerimônia, oferendas, coroas de espinhos,
marcas repetidas e preparação para um sacrifício. Os novos sigilos e a
geometria são autorais. A direção material segue a skill
`total-chaos-visual-direction` e a referência do
[Total Chaos clássico](https://www.moddb.com/mods/total-chaos/images/hotel-lobby).

## Alterações

- Pentagrama de aproximadamente 11,4 m, integrado a círculos de 14,3 m,
  inscrições e seis pontos de oferenda. Traços planos com bordas irregulares
  substituem o aspecto de barras metálicas e o anel segmentado anterior.
- Coroa de ferro no centro, tigelas, crânios e grupos de velas com pavio,
  chama, cera derretida e escorrimentos. O contorno tem uma emissão muito
  baixa, abaixo do limiar de bloom, para preservar a leitura nas sombras.
- Pré-arena: seis suportes de relíquias com crânios suspensos, duas mesas de
  preparação sobre as plataformas, estandartes, uma coroa suspensa, selo no
  piso e marcas de procissão até o portão. O concreto recebeu um material
  derivado mais escuro e terroso.
- Passagens X e Z: selos no piso e na parede, tigelas e velas laterais.
- Sala final: novos estandartes e duas pequenas concentrações de oferendas
  junto à parede de fundo. Os corpos pendurados e o símbolo do altar foram
  preservados.

Os props acrescentados são decoração estática, sem interação e sem novos
colisores. As superfícies de jogo e os colisores existentes são preservados.
Todas as luzes acrescentadas são locais e não projetam sombras.

## Assets e organização

Arquivos novos em `Assets/Art/Basement/RitualExpansion`. A geometria é nativa
da Unity, consolidada por material em cada ambiente. Não há dependência de
imagens geradas nem de plugins adicionais.

Materiais de ferro, pedra, cera, concreto, tecido e ossos reutilizam os assets
existentes do Basement. As texturas da Poly Haven e seus registros de origem
continuam nos manifestos anteriores de `Ritual`, `ArenaPolish` e `ArenaHorror`.
Nenhum asset oficial de Ordem Paranormal ou Total Chaos foi incorporado ao jogo.

O prefab `BasementFinalArena` está incorporado como objetos locais no
`Basement.prefab`; por isso, a nova decoração é aplicada a ambos. O sanctum
continua sendo um prefab aninhado. As referências e os IDs antigos são
preservados pelo instalador.

## Reprodução e reversão

Unity 6000.5.7f1 / URP 17.5. `prepare.py` sincroniza a cópia isolada existente
em `.utmp/BasementPreview` e preserva os três prefabs em
`.utmp/BasementRitualExpansion/before`. Não execute a preparação com esse
Editor isolado aberto; o Editor principal pode permanecer aberto.

Copie `BasementRitualExpansion.cs` para `Assets/Editor` da cópia isolada.
Os helpers `ChaosGeometry`, `BasementFinalBuilder` e `BasementSurfaceBuilder`
já presentes na cópia são necessários. Execute `BasementRitualExpansion.Build`.
`Refine` restaura somente os três prefabs da cópia isolada a partir do snapshot
e reconstrói o resultado, mantendo os GUIDs dos materiais e das malhas geradas.

Execute `python Tools/Basement/RitualExpansion/install.py` para mesclar apenas
filhos acrescentados, materiais e visibilidade dos renderizadores. Depois,
execute `BasementRitualExpansion.Verify` e `BasementSurfacePlayVerifier.Run`
na Unity isolada. O segundo método deve executar sem `-quit` e encerra o
Editor ao terminar o teste em Play Mode.

`install.py --install` exige logs válidos, prefabs idênticos aos validados e
ausência de alterações concorrentes nesses três arquivos. Os assets são
copiados antes dos prefabs. Para reverter, restaure os três prefabs do snapshot;
os materiais e texturas anteriores permanecem disponíveis.

## Validação e limites

Capturas `BeforeExpansion-*` e `VerifiedExpansion-*` usam as mesmas câmeras,
composição e atmosfera regional. `PentagramPlayer` usa a lanterna; `Pentagram`
mostra a inscrição de uma posição elevada para revisão. `Preparation` inspeciona
a decoração das plataformas; `PassageDetail` é uma captura final adicional
das oferendas junto ao piso. Relatórios técnicos em `validation.txt`,
`runtime-validation.txt` e `staging-report.json`.

O teste em Play Mode verifica atmosfera regional, lanterna, volumes,
oscilação das três luminárias antigas e restauração do ambiente externo.
Ele não simula uma partida de combate nem mede FPS em uma build.

Revisão visual provisória: 21/24 pela checklist da skill. Escala, materiais,
composição, fontes de luz, leitura das sombras, identidade e controle dos
efeitos estão presentes. Colisão dos props decorativos, variação adicional
de desgaste e desempenho no hardware alvo permanecem em 1/2. O piso central
e as linhas de navegação continuam livres de novos bloqueadores.
