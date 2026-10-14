# Refinamento da arena de Basement

Alterações no `Basement.prefab` e no prefab aninhado `BasementFinalArena.prefab`:

- Corpo humano anatômico, pele com UVs originais, cabeça virada, mortalha de
  tecido, cintas, mesa com rodas, instrumentos caídos e suporte de soro.
- Dois torsos decapitados e mutilados, extraídos da mesma malha anatômica da
  maca. Cabeças, braços e pernas foram destacados e espalhados ao redor, com
  feridas nos cortes e sangue. As partes não bloqueiam a rota.
- Maca tombada, volumes cobertos, cortina rasgada, sacos, entulho, pegadas,
  marcas de arrasto, infiltrações nas paredes e sujeira perto das bombas.
- Ambiente um pouco mais escuro e três luminárias defeituosas com oscilação
  independente. Os emissores visíveis acompanham a intensidade da luz por
  MaterialPropertyBlock, sem modificar materiais compartilhados.
- Novo perfil de exposição da arena salvo com os componentes do volume.
  Reflexo local estático atualizado; corpo da maca com dois níveis de LOD,
  partes da arena consolidadas por material.

Assets novos em `Assets/Art/Basement/ArenaHorror`. Os IDs antigos, geometria
de colisão, scripts de combate, portão e spawns são preservados pelo instalador.

## Origem dos assets

A malha humana, pesos e rig são dados gráficos do MakeHuman sob CC0. Os arquivos
originais e hashes ficam em `Source/MakeHuman`; a textura vem do pacote oficial
de skins. A licença oficial distingue os dados gráficos CC0 do código AGPL do
programa; este projeto usa os dados e código próprio de pose.

- [Licença oficial do MakeHuman](https://github.com/makehumancommunity/makehuman/blob/master/LICENSE.md)
- [Pacote oficial de skins](https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html)
- [Tecido terlenka](https://polyhaven.com/a/terlenka)
- [Detalhe de sujeira dirty_carpet](https://polyhaven.com/a/dirty_carpet)
- [Licença CC0 da Poly Haven](https://polyhaven.com/license)

O manifesto `Assets/Art/Basement/ArenaHorror/texture-sources.json` registra
origem, autores e hashes das texturas. Os materiais de concreto/metal reutilizam
as texturas CC0 já importadas em `Assets/Art/Basement/Surface`.

## Ferramentas deste pass

`prepare_horror.py` criou a cópia isolada e o snapshot antes das alterações;
ele recusa sobrescrever esse snapshot. `download_human.py` e `download_linen.py`
obtêm os dados gráficos. `pose_human.py` gera as malhas com NumPy e
`cut_human.py` separa cabeça, braços e pernas, preservando UVs e normais. A pose
também pode usar os dados originais locais em `Source/MakeHuman`.

No projeto isolado preparado em `.utmp/BasementPreview`, Unity 6000.5.7f1:

```powershell
python Tools/Basement/Horror/pose_human.py
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Basement/Horror/run_horror.ps1 -Phase build
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Basement/Horror/run_horror.ps1 -Phase validate
```

O build restaura os dois prefabs do snapshot deste pass antes de reconstruir.
O instalador exige logs aprovados e hashes dos assets originais antes de copiar
para o projeto principal; ele deve ser executado uma única vez contra esse
snapshot. Os protótipos sem uso não são instalados: somente dependências finais
e texturas com suas fontes.

Capturas e resultados finais em `Horror-*.png`, `capture-metrics.txt`,
`dismemberment-metrics.json`,
`runtime-validation.txt`, `arena-flicker-validation.txt` e `validation.md`.
