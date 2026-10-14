# Basement — materiais e atmosfera de Total Chaos clássico

Unity 6000.5.7f1 / URP 17.5. Aplicação da skill `total-chaos-visual-direction`
fornecida pelo usuário, do prompt PBR e da captura de referência.
O cenário continua sendo uma instalação subterrânea: os tons frios e a
profundidade da referência foram adaptados às luminárias industriais existentes.

- 179 renderizadores usam o shader existente `UniDoom/TotalChaosInspiredSurface`.
  Albedo, normal, AO, metal/smoothness, sujeira nas cavidades e umidade seletiva;
  texturas trilineares com mipmaps, anisotropia 4 e compressão de alta qualidade.
  Parallax permanece opcional e desligado. Materiais de sangue, corpos, símbolos,
  sinalização e outros acabamentos especiais conservam suas referências.
- 16 materiais exclusivos; os assets antigos permanecem intactos. O conversor
  existente aceita uma pasta de destino e uma biblioteca de texturas reutilizada.
  **Tools → Total Chaos → Material Converter → Revert converted references**
  restaura os materiais originais por GUID, incluindo as cópias das lâmpadas.
- Névoa regional: arena 0.014, sala final 0.018, corredor 0.009. Vinheta 0.14,
  grain 0.065–0.08, bloom 0.07 e tonemapping Neutral; sem aberração cromática.
  A oclusão de contato usa o SSAO já ativo no renderer PC e os mapas AO.
- Luzes localizadas, três fontes com sombras e a lanterna do jogador. Oscilações
  antigas foram desativadas; nenhuma lâmpada de navegação sofre apagões aleatórios.
  Dois cubemaps estáticos de 128 px, probes locais com projeção de caixa e
  intensidade 0.3. Nenhuma recaptura de reflexos por frame.
- O corredor tem overrides de posição e escala na cena principal. As duas regiões
  agora referenciam seus pisos. O adaptador regional sincroniza seus limites e
  os triggers de Volume com essas referências, usando um cache de matrizes.
  Os outros ambientes não usam essa opção; suas configurações permanecem iguais.

## Texturas

Powered by [Poly Haven](https://polyhaven.com). Os assets são
[CC0-1.0](https://polyhaven.com/license).

| Conjunto | Resolução | Uso |
|---|---|---|
| [Concrete Floor 02](https://polyhaven.com/a/concrete_floor_02) | 2K | Piso de concreto deteriorado; download novo |
| [Concrete Wall 006](https://polyhaven.com/a/concrete_wall_006) | 2K | Paredes e teto; biblioteca existente |
| [Rusty Metal 02](https://polyhaven.com/a/rusty_metal_02) | 1K | Tubos, estruturas, equipamentos e barris |
| [Rock Face](https://polyhaven.com/a/rock_face) | 1K | Elementos de pedra |

A biblioteca existente também mantém Wood Table Worn para o preset reutilizável
de madeira. Não há um material de madeira convertido neste Basement.
`Assets/Art/Basement/Surface/texture-sources.json` registra URLs oficiais,
autores, licença, resolução, tamanho e SHA-256. Os PNGs baixados são preservados.
ARM é convertido em máscara Unity: R metallic, G AO, B roughness, A smoothness.
Albedo é sRGB; normais GL e máscaras são lineares. Height é importado para o
controle opcional de parallax, sem custo de amostragem enquanto desativado.

## Capturas e validação

[Arena antes](Before-Arena.png) · [Arena final](Final-Arena.png) ·
[Tubulações](Final-Pipes.png) · [Sala final](Final-Boss.png) ·
[Lanterna](Final-Flashlight.png) · [Corredor](Final-Corridor.png).

As comparações usam câmera de 70°, altura de 1.84 m e resolução 1672 × 941.
Arena e sala final são capturadas sem lanterna; os detalhes têm lanterna ligada.
O corredor final usa o piso real da cena; a antiga captura do corredor, situada
fora da passagem devido aos volumes desatualizados, não é uma comparação válida.

Resultados e limites em [validation.md](validation.md). A cena principal e o
player não são salvos pelas ferramentas. Os três prefabs conservam os IDs antigos,
malhas, colisão sólida, scripts de combate e hierarquias; somente probes de
reflexão e dados auxiliares de luz são adicionados.

## Reconstrução

1. Faça um snapshot do estado atual; `prepare.py` sincroniza a cópia isolada
   `.utmp/BasementPreview` e registra hashes. Não sobrescreva snapshots anteriores.
2. Execute `download_textures.py` e copie `BasementSurfaceBuilder.cs` para o
   diretório `Assets/Editor` da cópia isolada. Copie também o conversor e o runtime
   atuais. `BasementSurfaceBuilder.Build` cria materiais e iluminação;
   `BasementSurfaceBuilder.Polish` calibra metais e gera os cubemaps.
3. `run_validation.ps1` copia as ferramentas, associa os volumes aos pisos,
   mescla apenas dados visuais no YAML e executa validações finais e Play Mode.
4. `install_surface.py --install` exige os logs de sucesso e verifica alterações
   concorrentes antes de copiar os prefabs e os assets desta revisão.

O backup anterior desta execução está em `.utmp/Basement-PBR/before`. Para reverter
o pass completo, restaure os três prefabs desse snapshot. As regiões antigas não
possuem referências ao piso e o adaptador mantém o comportamento anterior.
O conversor e o runtime anteriores também estão preservados no snapshot.

Referências visuais: captura enviada pelo usuário,
[Total Chaos no ModDB](https://www.moddb.com/mods/total-chaos/images/hotel-lobby) e
[serviços industriais do mod clássico](https://media.moddb.com/images/members/1/314/313486/profile/tc3.png).
Os assets do mod são apenas referências visuais e não foram incorporados ao projeto.
