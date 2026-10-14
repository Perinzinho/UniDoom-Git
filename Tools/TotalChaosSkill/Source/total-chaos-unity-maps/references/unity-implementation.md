# Implementação na Unity

Leia quando for entregar scripts ao usuário, quando ele reportar erro/visual errado, ou quando
pedir extensões (andares, escadas, props próprios, NavMesh, lanterna, interação).

## Índice
1. Requisitos
2. Instalação (5 passos)
3. Uso
4. O que o builder gera
5. Afinação do visual
6. Desempenho
7. Gameplay: porta, chave, lanterna, NavMesh
8. Troubleshooting
9. Extensões comuns

---

## 1. Requisitos

- **Unity 6 (6000.x) ou 2022.3 LTS+** com **URP** (template "Universal 3D"). O código usa
  `Object.FindObjectsByType` (Unity ≥ 2022.2) e as classes de pós-processamento do URP.
- Pipeline **Built-in**: funciona, mas apague a 1ª linha (`#define TC_URP`) de `TCAtmosphere.cs`;
  o pós-processamento deve então ser montado à mão com o Post Processing Stack v2.
- HDRP não é suportado por esta skill.
- Os scripts foram escritos sem acesso a uma Unity para compilar. Se aparecer erro de
  compilação, peça ao usuário a mensagem **completa** (arquivo, linha, código CSxxxx) e corrija
  a causa — não reescreva tudo.

## 2. Instalação (5 passos)

1. Copie `assets/unity/Runtime/*.cs` para `Assets/TotalChaos/Runtime/`.
2. Copie `assets/unity/Editor/*.cs` para `Assets/TotalChaos/Editor/` (a pasta **precisa** se
   chamar `Editor`).
3. Salve o layout JSON em qualquer lugar (ex.: `Assets/TotalChaos/Maps/meu_mapa.json`).
4. Abra uma cena vazia (com câmera `MainCamera`) e use **Tools ▸ Total Chaos Maps ▸ Build Map
   From JSON...**, escolhendo o arquivo. Para testar a instalação use **Build Example Map**.
5. Posicione o jogador em `Markers/PlayerStart`, aperte Play.

Se o projeto usa Assembly Definitions: crie `TotalChaos.Runtime.asmdef` na pasta Runtime e
`TotalChaos.Editor.asmdef` (plataforma só Editor, referenciando Runtime, `Unity.RenderPipelines.Core.Runtime`
e `Unity.RenderPipelines.Universal.Runtime`).

## 3. Uso

- **Rebuild:** rodar o builder de novo apaga o GameObject raiz de mesmo nome e recria. Materiais
  gerados **não** são sobrescritos (suas trocas de textura sobrevivem). Meshes são recriadas.
- **Menu Atmosphere:** reaplica fog/ambient/pós-processamento de um tema sem reconstruir o mapa.
- Campos do JSON: `name`, `theme`, `cellSize`, `wallHeight`, `doorHeight`, `ceiling`, `autoShell`,
  `textureMeters`, `seed`, `grid` (detalhes e legenda no SKILL.md).
- `seed` controla variações aleatórias (luzes que piscam, rotação/tamanho das caixas): mesmo
  seed → mesmo mapa.

## 4. O que o builder gera

```
TC_Mapa/
├─ Geometry/  Walls, Floor, Ceiling   (3 malhas estáticas com MeshCollider; UV em metros)
├─ Doors/     Door, LockedDoor_1…     (cubo + Rigidbody kinematic + TCSimpleDoor)
├─ Lights/    Light …                 (point lights; algumas com TCFlickerLight)
├─ Markers/   PlayerStart, EnemySpawn, ItemSpawn[_key_N], Exit   (TCMapMarker + gizmo)
└─ Props/     Crate, Barrel, CeilingLamp, StreetLamp*, KeyVisual_N
```
Além disso, em cena: `TC_PostFX` (Volume global) e, em tema `streets`, `TC_Sun`. A luz
direcional padrão da cena é zerada em temas fechados.

Assets em `Assets/TotalChaosMaps/Generated/{Materials,Textures,Meshes,Profiles}`.

Coordenadas: norte (primeira linha do JSON) = +Z; leste = +X; 1 célula = `cellSize` metros.
O grid ganha 1 célula de padding em volta, então a coluna 0 do JSON fica em X = 0.

## 5. Afinação do visual

Tudo parte de `TCTheme.cs` (cores, densidade de fog, intensidade/alcance das luzes, saturação).
Receitas rápidas:

| Problema | Ajuste |
|---|---|
| Escuro demais para testar | `ambient` do tema ↑ (#1a1c1a) **ou** lanterna no jogador; evite subir `exposure` |
| Fog não "esconde" o fim | `fogDensity` ↑ 0.01–0.02 |
| Limpo demais | `streak`/`dirt` em `TCAssets.Surface(...)` ↑; adicionar decals; grain ↑ 0.7 |
| Poças de luz pouco marcadas | `lightRange` ↓ 7, `lightIntensity` ↑ 0.3, ambient ↓ |
| Cores vivas demais | `saturation` ↓ (-45) |
| Textura borrada | selecionar a textura → Filter Mode = Point; sem anisotropia |
| Visual mais retrô | Render Scale 0.5 no URP Asset (veja `art-direction.md §11`) |

## 6. Desempenho

- Interiores com muitas point lights: URP **Forward+** (Unity 6) lida bem com dezenas; em
  Forward clássico limite a 8 luzes adicionais por objeto (URP Asset ▸ Lighting) e mantenha
  `lightRange` curto.
- Sombras só em luzes-chave. Sombras de point light custam 6 passes.
- Para mapas grandes (> 60×60 células) use **Occlusion Culling** (Window ▸ Rendering ▸ Occlusion
  Culling ▸ Bake) e divida o mapa em vários JSON/cenas aditivas.
- Para lightmap/baked GI: gere UV2 nas meshes (`Unwrapping.GenerateSecondaryUVs` ou marque
  "Generate Lightmap UVs" ao salvar como modelo) — o builder não faz por padrão porque o estilo
  alvo usa luz dinâmica.

## 7. Gameplay

**Abrir portas / chaves** (exemplo mínimo no jogador):

```csharp
using System.Collections.Generic;
using UnityEngine;
using TotalChaosMaps;

public class PlayerUse : MonoBehaviour
{
    public Camera cam;
    public float reach = 2.5f;
    readonly HashSet<string> keys = new HashSet<string>(); // "1", "2"...

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, reach))
        {
            var door = hit.collider.GetComponentInParent<TCSimpleDoor>();
            if (door != null && !door.Interact(k => keys.Contains(k)))
                Debug.Log("Trancada. Precisa da chave " + door.keyId);
        }
    }

    public void PickKey(int n) => keys.Add(n.ToString());
}
```
(Projetos com o novo Input System: troque `Input.GetKeyDown` por `Keyboard.current.eKey.wasPressedThisFrame`.)

**Chaves e itens:** os marcadores `ItemSpawn` com rótulo `key_N` indicam onde instanciar o
pickup da chave N; o builder só coloca um cubo emissivo como placeholder visual.

**Inimigos:** instancie seus prefabs nos `EnemySpawn`. Para navegação:
Package Manager ▸ **AI Navigation** ▸ adicione `NavMeshSurface` ao GameObject `Geometry`
(Collect Objects = Children) e **Bake**. Portas são obstáculos dinâmicos: adicione
`NavMeshObstacle` (Carve) se a IA precisar respeitá-las.

**Lanterna:** Spot Light filha da câmera (ângulo 60°, alcance 16, intensidade 3.5, sombras
Hard, cor `#fff2d0`), ligada/desligada com tecla; sutil balanço no `Update` reforça o clima.

## 8. Troubleshooting

| Sintoma | Causa provável / correção |
|---|---|
| Materiais rosa | Pipeline errado. Os materiais usam `Universal Render Pipeline/Lit` se existir, senão `Standard`. Apague `Assets/TotalChaosMaps/Generated/Materials` e rebuilde; ou use Edit ▸ Rendering ▸ Materials ▸ Convert. |
| Erro CS0246 `Vignette`/`Volume` | Projeto sem URP: apague `#define TC_URP` na 1ª linha de `TCAtmosphere.cs`. |
| Sem grain/vinheta | Câmera ▸ Rendering ▸ **Post Processing** ligado; o Volume `TC_PostFX` deve estar ativo e `Is Global`. |
| Cena toda preta | Ambient baixo + sem lanterna: normal. Adicione lanterna ou suba o ambient para testar. |
| Fog não aparece | Window ▸ Rendering ▸ Lighting ▸ Environment ▸ Fog marcado (o builder liga via script; confira se outra cena aditiva sobrescreve). |
| Paredes sem colisão | MeshCollider usa a malha gerada; confira `Geometry/Walls` e a camada/Physics. |
| Porta atravessável | Falta Rigidbody/colisor? Deve ser cubo + BoxCollider + Rigidbody kinematic. |
| "Layout vazio" | JSON sem campo `grid` ou com vírgula sobrando (JSON inválido). Valide com `validate_layout.py`. |
| Paredes com buracos | `autoShell` desligado e área andável tocando vazio. Ligue `autoShell`. |

## 9. Extensões comuns (quando o usuário pedir)

- **Vários andares / escadas / rampas:** adicione `levels: [{ y, grid }]` ao layout e uma
  célula `/`-`\` de rampa; o builder atual é de andar único. Implemente no `TCMapBuilder`
  repetindo o passo de geometria por nível com offset Y e criando rampas como caixas rotacionadas.
- **Props próprios (prefabs):** mapeie `C`/`O` e novos caracteres para prefabs num ScriptableObject
  e instancie com `PrefabUtility.InstantiatePrefab` no lugar de `Prim(...)`.
- **ProBuilder:** troque `MeshAcc` por `ProBuilderMesh` se o usuário quiser editar à mão depois.
- **Decals de sujeira:** URP Decal Renderer Feature + `DecalProjector` em pontos aleatórios de
  parede perto de `L` (manchas) e de `E` (sangue).
- **Múltiplos temas num mapa:** adicione campo de região/tema ao layout e Volumes locais
  (Volume com Box Collider) por região.
