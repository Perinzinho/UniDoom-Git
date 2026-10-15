# Plano de Reorganização e Limpeza do Projeto Unity
**Data:** 15/10/2026  
**Versão Unity:** 6000.5.7f1  
**Objetivo:** Padronizar a estrutura do projeto, organizar os recursos por cenário, centralizar assets compartilhados e remover arquivos obsoletos sem comprometer o funcionamento do jogo.

---

## 1. Objetivo principal

Reorganizar o projeto Unity seguindo quatro princípios:

1. **Organização por cenário:** cada ambiente terá sua própria pasta, contendo todos os recursos exclusivos daquele cenário.
2. **Centralização global:** recursos utilizados por múltiplos ambientes ou sistemas serão armazenados em uma pasta compartilhada.
3. **Limpeza segura:** scripts temporários, arquivos redundantes e assets comprovadamente desnecessários serão enviados para uma quarentena.
4. **Preservação do projeto:** todas as referências, GUIDs, configurações e funcionalidades existentes deverão ser mantidas.

A reorganização não deve alterar o comportamento, a aparência ou a jogabilidade dos ambientes.

## 2. Auditoria inicial

A auditoria foi concluída antes da reorganização.

| Item | Resultado |
|---|---|
| Arquivos originais em `Assets/` | 3.636 |
| Tamanho aproximado | 1,08 GB |
| Cenas identificadas | 7 |
| Prefabs | 31 |
| Scripts C# | 50 |
| Versão Unity | 6000.5.7f1 |
| Operações de movimentação planejadas | 1.452 |

**Arquivos de auditoria:**

- `audit-before.json` — inventário estático dos arquivos.
- `unity-before.json` — dependências reais obtidas pela Unity e resultados da abertura de cenas e prefabs.
- `classification.json` — classificação dos arquivos.
- `move-plan.json` — operações de movimentação planejadas.
- `path-map.json` — correspondência entre caminhos originais e novos.

Todos esses registros deverão ser preservados para validação e reversão.

## 3. Estrutura de pastas desejada

A estrutura principal deverá seguir este padrão:

```text
Assets/
│
├── _Project/
│   │
│   ├── Scenes/
│   │   ├── Main/
│   │   │   └── SampleScene.unity
│   │   │
│   │   ├── Basement/
│   │   ├── ClassRoom/
│   │   ├── Cylinder/
│   │   ├── DirectorRoom/
│   │   ├── EntranceGarden/
│   │   ├── EntranceHallway/
│   │   ├── Library/
│   │   │
│   │   └── _Recovery/
│   │
│   ├── Global/
│   │   ├── Audio/
│   │   ├── Fonts/
│   │   ├── Materials/
│   │   ├── Meshes/
│   │   ├── Prefabs/
│   │   ├── Scripts/
│   │   ├── Shaders/
│   │   ├── Sprites/
│   │   ├── Textures/
│   │   ├── Profiles/
│   │   └── Settings/
│   │
│   └── Editor/
│       └── Tools/
│
├── TextMesh Pro/
├── ProBuilder Data/
└── [Outros conteúdos preservados]
```

Cada pasta de cenário poderá conter a seguinte estrutura, criada apenas quando houver arquivos correspondentes:

```text
NomeDoCenario/
│
├── NomeDoCenario.unity
├── Materials/
├── Meshes/
├── Textures/
├── Profiles/
├── Prefabs/
├── Lighting/
├── Terrain/
├── Scripts/
└── Settings/
```

Não criar pastas vazias desnecessariamente.

### 3.1. Cenas principais

Mover os ambientes para:

- `Scenes/Basement/`
- `Scenes/ClassRoom/`
- `Scenes/Cylinder/`
- `Scenes/DirectorRoom/`
- `Scenes/EntranceGarden/`
- `Scenes/EntranceHallway/`
- `Scenes/Library/`

A cena agregadora `SampleScene` ficará em `Scenes/Main/`.

As duas cenas de recuperação permanecerão preservadas em `Scenes/_Recovery/`.

### 3.2. Assets exclusivos

Materiais, meshes, texturas e profiles exclusivos de um ambiente deverão ficar dentro da pasta correspondente.

As coleções `SkillAtmosphere` e `TotalChaosInspired` deverão ser organizadas nos respectivos ambientes.

Preservar as subpastas de etapas de arte quando necessário para:

- Evitar colisões de nomes.
- Manter a procedência dos arquivos.
- Preservar configurações de importação.
- Respeitar caminhos relativos de modelos e recursos associados.

### 3.3. Assets globais

Recursos compartilhados por múltiplos ambientes ou sistemas deverão ser movidos para `Assets/_Project/Global/`.

Isso inclui, quando aplicável:

- Texturas compartilhadas.
- Scripts de gameplay e sistemas.
- Prefabs globais.
- Áudio.
- Fontes.
- Sprites.
- Shaders.
- Materiais e meshes compartilhados.
- Profiles e configurações gerais.

Preservar a procedência das texturas no caminho de destino quando necessário.

Não duplicar assets que possam ser compartilhados.

### 3.4. Arquivos que não devem ser reorganizados

Manter nos locais atuais:

- TextMesh Pro.
- ProBuilder Data.
- Visual Scripting.
- Template `TutorialInfo`.

Esses conteúdos não apresentam necessidade demonstrada de reorganização.

Não modificar pacotes externos ou sistemas internos do Unity sem justificativa técnica.

## 4. Organização dos scripts e ferramentas

### 4.1. Scripts permanentes

Manter os scripts necessários para:

- Gameplay.
- Sistemas globais.
- Configuração de cenas.
- Comportamentos em runtime.
- Ajustes persistentes de layout e encounters.
- Ferramentas de Editor ainda utilizadas.

Scripts específicos de um único ambiente poderão ser armazenados dentro da pasta desse cenário.

Scripts compartilhados deverão ficar em `Global/Scripts/`.

### 4.2. Ferramentas de Editor

Ferramentas de desenvolvimento ainda úteis deverão ser organizadas em:

`Assets/_Project/Editor/Tools/`

Atualizar caminhos textuais utilizados por:

- Configuração de build.
- Conversores de assets.
- Ferramentas de auditoria mantidas.
- Sistemas que constroem caminhos para texturas compartilhadas.

Verificar referências por string que não são corrigidas automaticamente pela movimentação dos assets.

### 4.3. Scripts temporários de geração

Scripts utilizados exclusivamente para construir mapas, gerar meshes, posicionar objetos ou criar materiais devem ser avaliados individualmente.

Só poderão ser descartados quando:

1. Sua execução já estiver concluída.
2. Os resultados necessários estiverem salvos permanentemente.
3. Não houver dependências relevantes.
4. Não forem necessários em runtime.
5. Não forem necessários para futuras regenerações do cenário.

**Exceções identificadas:**

- Manter os três scripts de cenário que ainda ajustam layout ou encounters.
- Preservar `MedkitPrefabCreator`, pois seu resultado esperado ainda não está salvo como prefab e não foi comprovado que a ferramenta é descartável.

## 5. Limpeza e quarentena

Criar uma área de descarte fora da pasta `Assets/`:

`_Quarantine/2026-10-15/`

Essa pasta não deverá participar da importação de assets nem do build.

Todo descarte deverá preservar o arquivo original e seu `.meta` correspondente, permitindo recuperação.

### 5.1. Scripts que poderão ser arquivados

Os seguintes scripts e componentes foram selecionados explicitamente para arquivamento:

- `TCMapBuilder`
- `TCAssets`
- `TCTheme`
- `TCAtmosphere`

A auditoria não identificou dependências externas desses scripts nos scripts mantidos, cenas ou prefabs.

As fontes também permanecem disponíveis em:

`Tools/TotalChaosSkill/Source`

### 5.2. Meshes redundantes

Enviar para quarentena as 30 meshes persistentes identificadas como redundantes.

Condições verificadas:

- Ausência de referências de entrada em `Assets/`, configurações e dependências obtidas pela Unity.
- Campos serializados idênticos aos de meshes utilizadas, exceto o nome.
- Nenhum consumidor depende das cópias descartadas.

Não remapear referências desnecessariamente.

### 5.3. Arquivos que deverão ser preservados

Não descartar:

- Texturas aparentemente duplicadas utilizadas por caminhos construídos pelo conversor.
- Materiais originais necessários à preservação e reversão.
- JSONs de auditoria e recuperação.
- Scripts com utilização comprovada.
- Assets cuja utilização não tenha sido determinada com segurança.

### 5.4. Classificação de arquivos

O arquivo `classification.json` deverá manter as cinco categorias:

| Classificação | Ação |
|---|---|
| MANTER | Preservar o arquivo |
| REORGANIZAR | Mover para a pasta adequada |
| GLOBAL | Mover para a estrutura compartilhada |
| DESCARTE SEGURO | Enviar para quarentena |
| REVISÃO MANUAL | Preservar até análise posterior |

Arquivos classificados como **REVISÃO MANUAL** não poderão ser descartados automaticamente, embora possam acompanhar a pasta correspondente ao ambiente.

## 6. Regras para movimentação segura

Todas as movimentações dentro de `Assets/` deverão utilizar preferencialmente `AssetDatabase.MoveAsset`.

Preservar:

- GUIDs.
- FileIDs.
- Arquivos `.meta`.
- Configurações de importação.
- Dependências entre assets.
- Referências em cenas e prefabs.
- Bindings de materiais e meshes.

Não modificar o conteúdo dos assets durante a organização, exceto quando for indispensável atualizar caminhos textuais.

O manifesto `move-plan.json` define as 1.452 operações previstas.

O `path-map.json` deverá registrar todos os arquivos individualmente, inclusive aqueles movimentados dentro de pastas inteiras.

## 7. Validação obrigatória

Após a reorganização, executar uma auditoria de comparação entre o estado original e o estado final.

### 7.1. Integridade dos arquivos

Comparar:

- Hashes dos conteúdos.
- GUIDs e FileIDs.
- Dependências recursivas.
- Quantidade de objetos e componentes.
- Referências de materiais.
- Referências de meshes.
- Configurações de importação.

### 7.2. Integridade das cenas e prefabs

Verificar:

- Abertura de todas as cenas.
- Abertura de todos os prefabs.
- Referências ausentes.
- Scripts ausentes.
- Bindings de materiais e meshes.
- Alterações inesperadas de componentes.

### 7.3. Compilação e build

Executar:

1. Compilação dos scripts C#.
2. Verificação de erros e avisos.
3. Development Build para Windows.
4. Registro dos resultados.

Não declarar Play Mode, comparação visual ou testes de desempenho como concluídos sem sua execução real.

### 7.4. Problemas preexistentes

A auditoria inicial identificou:

- Zero scripts ausentes.
- Referências ausentes nas duas cenas de recuperação.
- Referências ausentes em `Gun.prefab`.
- Componentes `MeshFilter` vazios preexistentes.

Esses problemas deverão ser documentados e separados de eventuais problemas introduzidos pela reorganização.

Não atribuir problemas antigos à movimentação dos arquivos.

## 8. Preservação das alterações do usuário

Antes da reorganização, foram identificadas 43 alterações de materiais salvas pelo usuário ao fechar a Unity.

Essas alterações devem permanecer intactas.

Os registros estão armazenados em:

- `saved-before-organization.patch`
- `.utmp/ProjectOrganization/saved-before/`

Não sobrescrever, reverter ou descartar essas alterações.

## 9. Sistema de reversão

Toda movimentação deverá ser registrada em um journal.

O sistema de reversão deverá permitir:

- Restaurar os caminhos originais.
- Recuperar arquivos da quarentena.
- Preservar os GUIDs.
- Recuperar associações entre arquivos e seus `.meta`.
- Identificar alterações realizadas após a reorganização.
- Impedir que uma reversão sobrescreva modificações posteriores sem autorização.

Não excluir arquivos permanentemente.

Não criar commits automaticamente.

## 10. Ordem de execução

A implementação deverá seguir estas etapas:

### Fase 1 — Preparação

- Confirmar os arquivos de auditoria.
- Preservar as alterações do usuário.
- Verificar o manifesto de movimentação.
- Preparar o journal e a estrutura de recuperação.

### Fase 2 — Reorganização dos cenários

- Criar a estrutura principal.
- Mover as cenas para seus ambientes.
- Organizar assets exclusivos.
- Preservar as subpastas necessárias.

### Fase 3 — Centralização global

- Identificar recursos compartilhados.
- Organizar `Global/` por tipo.
- Atualizar caminhos textuais necessários.
- Conferir dependências.

### Fase 4 — Organização das ferramentas

- Reorganizar scripts permanentes.
- Centralizar ferramentas de Editor.
- Preservar geradores ainda necessários.
- Ajustar configurações e caminhos dependentes.

### Fase 5 — Quarentena

- Arquivar os scripts selecionados.
- Mover as 30 meshes redundantes.
- Preservar arquivos classificados como revisão manual.
- Registrar as movimentações.

### Fase 6 — Validação

- Executar auditoria posterior.
- Comparar resultados com o baseline.
- Verificar cenas e prefabs.
- Compilar o projeto.
- Executar o Development Build Windows.
- Documentar divergências.

### Fase 7 — Relatório final

Apresentar:

- Estrutura final das pastas.
- Quantidade de arquivos movidos.
- Quantidade de arquivos enviados à quarentena.
- Recursos globais organizados.
- Scripts mantidos e arquivados.
- Dependências verificadas.
- Resultado da compilação e do build.
- Problemas preexistentes.
- Problemas novos, caso existam.
- Arquivos pendentes de revisão manual.
- Instruções de reversão.

## 11. Regras permanentes para futuras gerações

Após a reorganização, toda IA que modificar o projeto deverá seguir estas regras:

1. Cada novo cenário deve ter sua própria pasta em `Scenes/`.
2. Assets exclusivos devem ficar dentro da pasta do cenário.
3. Assets compartilhados devem ficar em `Global/`.
4. Não criar materiais, meshes ou texturas duplicados sem necessidade.
5. Não criar scripts geradores descartáveis permanentemente para tarefas pontuais.
6. Reutilizar ferramentas de geração existentes sempre que possível.
7. Salvar os resultados da geração em assets persistentes.
8. Descartar geradores temporários somente depois de validar que não são mais necessários.
9. Preservar GUIDs, arquivos `.meta` e referências.
10. Não modificar arquivos de pacotes externos sem necessidade.
11. Não excluir definitivamente arquivos sem autorização.
12. Não quebrar o projeto para melhorar sua organização.

---

## 12. Critérios de conclusão

A reorganização só será considerada concluída quando:

- Todos os cenários estiverem organizados.
- Os assets compartilhados estiverem centralizados.
- As ferramentas permanentes estiverem corretamente localizadas.
- Os arquivos descartáveis validados estiverem em quarentena.
- Nenhum arquivo necessário tiver sido perdido.
- Não houver novas referências quebradas.
- A compilação e o build tiverem seus resultados documentados.
- O sistema de reversão estiver disponível e verificável.
- As alterações anteriores do usuário permanecerem preservadas.

**Resultado esperado:** um projeto Unity organizado, limpo, escalável, com recursos separados por cenário e globais, sem acúmulo desnecessário de scripts de geração e com segurança para futuras modificações.
