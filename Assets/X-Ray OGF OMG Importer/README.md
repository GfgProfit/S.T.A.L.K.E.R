# X-Ray OGF/OMF Importer for Unity 6

Нативный импорт моделей `*.ogf` и анимаций `*.omf` движка X-Ray (S.T.A.L.K.E.R.) через
`ScriptedImporter`. Blender, Python, FBX и внешние конвертеры при импорте не нужны.

Требования: Unity 6000.3.x. HDRP 17.x нужен только для материалов (`HDRP/Lit`); без HDRP
используется материал по умолчанию текущего пайплайна.

## Установка

Папка `Assets/X-Ray OGF OMG Importer` самодостаточна: скопировать её в `Assets` другого проекта.

| Сборка | Платформы | Содержимое |
| --- | --- | --- |
| `XRay.Importer.Core` | все, `noEngineReferences` | чтение OGF/OMF, математика скелета (double), эталонный скиннинг |
| `XRay.Importer.Runtime` | все | `XRayModel`, `XRayMotionLibrary`, `XRayRigPlayer`, `XRayHudAnimator` |
| `XRay.Importer.Editor` | Editor | импортёры, построение Unity-объектов, клипов, материалов, инспекторы |
| `XRay.Importer.Tests.Editor` | Editor, `UNITY_INCLUDE_TESTS` | EditMode-тесты |

`Tools~/blender_export_reference.py` — инструмент разработки (Unity его не импортирует).

## Использование

1. Положить `*.ogf` и `*.omf` в `Assets`. OGF импортируется как префаб: корень с `XRayModel`
   и `Animator`, иерархия костей с исходными именами, по объекту `mesh_NN_<texture>` на каждый
   visual (`SkinnedMeshRenderer` или `MeshRenderer`), материалы и меши — саб-ассеты.
2. OMF импортируется как `XRayMotionLibrary` (главный ассет) + `AnimationClip` на каждое движение.
   Риг (OGF, задающий пути костей и масштаб) указывается в инспекторе OMF (`OGF Rig`) или
   определяется автоматически: OGF из той же папки, скелет которого содержит все кости OMF.
3. Переимпорт/изменение настроек OGF автоматически переимпортирует зависящие OMF
   (artifact dependency). Отдельных настроек координат у OMF нет — только масштаб рига.

Настройки OGF: `Scale`, `Add Animator`, `Readable Meshes`, `Update When Offscreen`,
`Generate HDRP Materials`, `Textures Folder` (папка проекта, соответствующая `gamedata/textures`),
`Search Textures By Name`, импорт встроенных в OGF движений.
Настройки OMF: риг, автоопределение рига, режим зацикливания (по флагу `STOP_AT_END`/всегда/никогда),
выбор отдельных движений.

### HUD: руки + оружие

```csharp
// На корне рук и корне оружия: XRayRigPlayer (Library = соответствующая XRayMotionLibrary).
// На общем объекте: XRayHudAnimator (Hands, Weapon).
hudAnimator.Play("ak74_idle", "idle", 0.1f);
hudAnimator.SetTime(0.5f);
```

`XRayRigPlayer` строит `PlayableGraph` (ручное обновление, кроссфейд через микшер),
`XRayHudAnimator` продвигает оба графа одним `deltaTime`, поэтому пары HUD-анимаций не расходятся.
Каждый OMF анимирует только свой скелет.

## Пространства и математика

Пространство модели X-Ray (левостороннее, +Y вверх, +Z вперёд) совпадает с пространством Unity,
поэтому конвертация — тождество плюс масштаб позиций (`Editor/Building/XRayUnitySpace.cs` — единственное
место перевода значений). Правила выведены из blender-xray и подтверждены численно:

* Rest pose (`formats/ogf/imp/ik.py`): blender-xray строит
  `M = M_parent · MATRIX_BONE⁻¹ · T(S·t) · Euler((−rx, −rz, −ry), 'YXZ') · MATRIX_BONE`,
  `S` — перестановка Y/Z, `MATRIX_BONE = S · diag(1, 1, −1)`. Сопряжение `Rz·Rx·Ry` (Blender) через `S`
  даёт `Ry(ry)·Rx(rx)·Rz(rz)` в нативном пространстве, т.е. `localPosition = t`,
  `localRotation = Quaternion.Euler(rx, ry, rz)` (в градусах). Порядок `Z·X·Y` из прежней диагностики —
  тот же поворот, записанный в осях Blender.
* Ключи OMF (`formats/omf/imp.py`): `P_blender = MATRIX_BONE · Π T(Z·t)·R(w, x, y, −z)`, что равно
  `S · Π T(t)·R(conj q) · Z`. Отсюда `localPosition = t`, `localRotation = (−x, −y, −z, w)/32767`
  (нормализуется). Rest pose в формулу ключей не входит: в blender-xray `xmat` сокращается с rest-матрицами
  Blender. Translation: `key · size + init`, 8/16 бит по флагу `KPF_T_HQ`.
* Bind pose = `bone.worldToLocalMatrix · mesh.localToWorldMatrix`, берётся из тех же Transform,
  которые анимируют клипы.
* Веса: последний вес 3/4-link неявный (`1 − Σ`), повторяющиеся кости суммируются (как в сумме скиннинга
  движка), отрицательные остатки округления обнуляются, затем нормализация.
* Индексы треугольников сохраняются в файловом порядке (CW front face, как в Unity); UV `v → 1 − v`.
* Клипы: 30 кадров/с, ключ `i` в момент `i/30`, линейные касательные, непрерывность знака кватернионов.

## Поддержка форматов

| Данные | Статус |
| --- | --- |
| OGF v4 (SoC/CS/CoP): SKELETON_ANIM, SKELETON_RIGID, SKELETON_GEOMDEF_ST/PM, NORMAL, PROGRESSIVE, HIERRARHY | реализовано; скелетные типы проверены на тестовых файлах |
| Вершины 1/2/3/4-link, статические `0x112` | 1-link и 4-link (с 3-link) проверены на тестовых файлах, 2-link — по коду blender-xray |
| SWI (progressive) | берётся LOD0, не проверено на реальных файлах |
| OGF v3 (pre-release) | читается по логике blender-xray, **не проверено**, при импорте выдаётся предупреждение |
| Level-визуалы (VCONTAINER/ICONTAINER/GCONTAINER, CHILDREN_L, TEXTURE_L), TREE, LOD, PARTICLE | явная ошибка импорта |
| Сжатые чанки (LZHUF) | явная ошибка импорта (лицензия LZHUF не позволяет перенос) |
| OMF S_SMPARAMS v1–v4, S_MOTIONS_2 | реализовано; v4 проверено |
| S_MOTIONS_0/1 (ранние билды) | явная ошибка импорта |
| Метки движений (v4), флаги, speed/power/accrue/falloff | сохраняются в `XRayMotionLibrary` |

Материалы: `HDRP/Lit` с `_BaseColorMap`; шейдеры X-Ray с `aref` → alpha clipping,
`transparent`/`glass`/`collimator` → прозрачная поверхность. Остальные параметры шейдеров X-Ray не переносятся.

## Тесты и верификация

Window → General → Test Runner → EditMode → `XRay.Importer.Tests.Editor`.
Тестовые данные: `Assets/test_xray` (если их нет — тесты помечаются Ignored, а не Passed).

* `FormatTests` — разбор реальных файлов, нормировка весов, порядок обхода треугольников, явные ошибки
  на повреждённых/неподдерживаемых данных, совпадение `FromXRayBindEuler` с `Quaternion.Euler`.
* `BlenderReferenceTests` — Core-математика против Blender: rest/pose-матрицы всех костей,
  локальные матрицы, габариты мешей (JSON владельца проекта) и поклоновершинное сравнение
  (`Assets/test_xray/blender_reference`, 27 кадров рук и 26 кадров оружия).
* `UnityImportTests` — импортированные ассеты: `AnimationClip.SampleAnimation` на префабе,
  матрицы Transform против Core и Blender, `SkinnedMeshRenderer.BakeMesh` против вершин Blender,
  изоляция ригов (клипы оружия не меняют руки и наоборот), пути привязок кривых.

Перегенерация эталона Blender:

```
blender -b --factory-startup --python "Tools~/blender_export_reference.py" -- <model.ogf> <motions.omf> <out_prefix> ak74_idle:0,15,50,99 ak74_reload
```

## Ограничения

* Вращения между ключами: Unity интерполирует компоненты кватерниона и нормализует (nlerp),
  движок — slerp. В ключах результат точный; между ключами (1/30 c) расхождение пренебрежимо мало.
* Зацикленные движения X-Ray интерполируют последний ключ к первому; клип Unity заканчивается
  на последнем ключе (длина `(N − 1)/30`, как в Blender).
* Текстуры, добавленные в проект после импорта OGF, требуют ручного Reimport.
* Отсутствуют: физические формы костей/джоинты как компоненты Unity, LOD-цепочки, level-геометрия.
