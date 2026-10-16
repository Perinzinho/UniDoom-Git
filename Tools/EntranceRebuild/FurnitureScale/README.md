# Escala do mobiliário da diretoria — 16/10/2026

Mesa e três cadeiras ampliadas uniformemente em 30% em relação ao resultado anterior. O player existente tem CharacterController com aproximadamente 2,121 m de altura e olhos a 1,839 m do piso. O player e as alterações existentes do usuário em SampleScene foram preservados.

| Objeto | Altura anterior | Altura ajustada |
|---|---:|---:|
| Mesa | 0,78 m | 1,014 m |
| Cadeiras de visita | 0,88 m | 1,144 m |
| Cadeira do diretor | 1,02 m | 1,326 m |

As posições foram ampliadas em torno do centro da mesa, mantendo a composição e o espaço para circulação. Luminária, luz da mesa, documentos e suporte de couro acompanham o ajuste. As escalas e posições persistem no array de placements de EntranceDressingLayout, evitando que o script restaure os tamanhos antigos ao abrir a cena ou entrar em Play Mode. Os colliders acompanham os transforms.

Somente o conteúdo do prefab `Assets/_Project/Scenes/DirectorRoom/Prefabs/DirectorRoom.prefab` foi alterado dentro de Assets. Não foram adicionados componentes, materiais ou objetos; GUID, fileIDs, hierarquia e os demais componentes foram preservados. O gerador histórico foi atualizado para reproduzir as proporções novas.

`Before.png`, `After.png` e `Verified.png` mostram a mesma vista no mapa, na altura dos olhos do player. Os arquivos de texto correspondentes registram medidas, contato com piso/tampo e passagem de entrada com cápsula. Compilação e Play Mode foram executados na cópia isolada; os spawners foram desativados apenas nessa cópia, como na validação anterior. Não foi executado novo build nem profiling de desempenho para este ajuste de transforms.

O backup imediatamente anterior está em `Before/`; `before.json` e `installed.json` registram a integridade e o resultado da instalação. Para uma reversão seletiva, confira se o hash atual do prefab ainda corresponde a `installed.json` antes de restaurar seu backup. Não utilize a reversão da primeira reconstrução para desfazer somente esta revisão.
