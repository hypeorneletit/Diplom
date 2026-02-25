# 📋 ПОЛНАЯ ИНСТРУКЦИЯ ПО НАСТРОЙКЕ СЕРВЕРОВ

## ✅ Что было исправлено

1. **Убрана proximity detection** - теперь работает только через нажатия
2. **Реализована система короткого/длинного нажатия** (как у камер наблюдения):
   - **Короткое нажатие** → открывает окно
   - **Длительное нажатие (≥1 секунда)** → закрывает окно
3. **Убрана interaction zone** - не нужна больше
4. **Прописаны все размеры и позиции** четко

---

## 🎯 ПОШАГОВАЯ НАСТРОЙКА (ОЧЕНЬ ПОДРОБНО)

### ШАГ 1: Настройка основного объекта сервера

**Для каждого из 4 серверов (servers-1, servers-2, servers-3, servers-3A):**

1. **Выберите основной объект сервера** (например, `servers-1`)
2. **Добавьте компонент `ServerController`**
3. **Настройте параметры:**

   ```
   Window Reference:
   - Server Window: НЕ ЗАПОЛНЯЙТЕ СЕЙЧАС (заполним после создания UI)
   
   Server Settings:
   - Server Index: 
     * servers-1 → 0
     * servers-2 → 1
     * servers-3 → 2
     * servers-3A → 3
   - Server Display Name: 
     * "Сервер 1" для servers-1
     * "Сервер 2" для servers-2
     * "Сервер 3" для servers-3
     * "Сервер 4" для servers-3A
   
   Visual Feedback (опционально):
   - Server Renderers: перетащите все Renderer компоненты сервера
   - Hover Material: материал для наведения (можно оставить None)
   - Normal Material: обычный материал сервера
   
   Input Settings:
   - Long Press Threshold: 1.0 (время длительного нажатия в секундах)
   ```

4. **Проверьте наличие XR Interactable:**
   - Если у сервера НЕТ `XRSimpleInteractable` или `XRBaseInteractable`, скрипт создаст его автоматически
   - Если есть - оставьте как есть

---

### ШАГ 2: Создание UI структуры для окна

**Для каждого сервера создайте следующую структуру:**

```
servers-X (основной объект)
└── ServerWindow (новый GameObject)
    ├── ServerCanvas (Canvas)
    │   ├── WindowPanel (Image)
    │   ├── ServerNameText (TextMeshProUGUI)
    │   ├── ServerStatusText (TextMeshProUGUI)
    │   ├── ServerDetailsText (TextMeshProUGUI)
    │   └── ActionHintText (TextMeshProUGUI)
```

#### 2.1. Создание ServerWindow

1. **Создайте пустой GameObject** под сервером
2. **Назовите его `ServerWindow`**
3. **Позиция:** `X: 0, Y: 0, Z: 0` (относительно родителя)
4. **Rotation:** `X: 0, Y: 0, Z: 0`
5. **Scale:** `X: 1, Y: 1, Z: 1`

#### 2.2. Создание ServerCanvas

1. **Под ServerWindow создайте Canvas:**
   - **Название:** `ServerCanvas`
   - **Добавьте компонент `Canvas`**
   - **Настройки Canvas:**
     ```
     Render Mode: World Space
     Event Camera: Main Camera (или ваша VR камера)
     ```
   - **Добавьте компонент `Canvas Scaler`:**
     ```
     UI Scale Mode: Constant Pixel Size
     Reference Pixels Per Unit: 100
     ```
   - **Добавьте компонент `Graphic Raycaster`** (если нет)

2. **Rect Transform для ServerCanvas:**
   ```
   Position: X: 0, Y: 0, Z: 0
   Width: 550
   Height: 750
   Anchors: Min (0.5, 0.5), Max (0.5, 0.5)
   Pivot: (0.5, 0.5)
   Scale: X: 0.001, Y: 0.001, Z: 0.001
   ```

#### 2.3. Создание WindowPanel (фон окна)

1. **Под ServerCanvas создайте Image:**
   - **Название:** `WindowPanel`
   - **Добавьте компонент `Image`**
   - **Настройки Image:**
     ```
     Source Image: None (или ваш спрайт фона)
     Color: R: 20, G: 20, B: 20, A: 230 (темно-серый с прозрачностью)
     Raycast Target: ✓ (включено)
     ```

2. **Rect Transform для WindowPanel:**
   ```
   Anchors: Min (0, 0), Max (1, 1) - Stretch-Stretch
   Left: 0
   Top: 0
   Right: 0
   Bottom: 0
   Pivot: (0.5, 0.5)
   ```

#### 2.4. Создание ServerNameText

1. **Под ServerCanvas создайте TextMeshProUGUI:**
   - **Название:** `ServerNameText`
   - **Добавьте компонент `TextMeshProUGUI`**

2. **Настройки TextMeshProUGUI:**
   ```
   Text: (оставьте пустым, заполнится скриптом)
   Font Asset: Roboto-Bold (или ваш шрифт)
   Font Size: 30
   Color: White (#FFFFFF)
   Alignment: Top Center (горизонтально: Center, вертикально: Top)
   Wrapping: Disabled
   Overflow: Overflow
   ```

3. **Rect Transform для ServerNameText:**
   ```
   Anchors: Min (0.5, 0.85), Max (0.5, 1.0)
   Width: 500
   Height: 50
   Pos X: 0
   Pos Y: 0
   Pivot: (0.5, 1.0)
   ```

#### 2.5. Создание ServerStatusText

1. **Под ServerCanvas создайте TextMeshProUGUI:**
   - **Название:** `ServerStatusText`
   - **Добавьте компонент `TextMeshProUGUI`**

2. **Настройки TextMeshProUGUI:**
   ```
   Text: (оставьте пустым)
   Font Asset: Roboto-Bold
   Font Size: 26
   Color: White
   Alignment: Top Left
   Wrapping: Disabled
   Overflow: Overflow
   ```

3. **Rect Transform для ServerStatusText:**
   ```
   Anchors: Min (0, 0.7), Max (1, 0.85)
   Left: 15
   Top: 0
   Right: -15
   Bottom: -5
   Pivot: (0, 1)
   ```

#### 2.6. Создание ServerDetailsText

1. **Под ServerCanvas создайте TextMeshProUGUI:**
   - **Название:** `ServerDetailsText`
   - **Добавьте компонент `TextMeshProUGUI`**

2. **Настройки TextMeshProUGUI:**
   ```
   Text: (оставьте пустым)
   Font Asset: Roboto-Bold
   Font Size: 24
   Color: White
   Alignment: Top Left
   Wrapping: Enabled ✓
   Overflow: Overflow
   ```

3. **Rect Transform для ServerDetailsText:**
   ```
   Anchors: Min (0, 0.25), Max (1, 0.7)
   Left: 15
   Top: 15
   Right: -15
   Bottom: -5
   Pivot: (0, 1)
   ```

#### 2.7. Создание ActionHintText

1. **Под ServerCanvas создайте TextMeshProUGUI:**
   - **Название:** `ActionHintText`
   - **Добавьте компонент `TextMeshProUGUI`**

2. **Настройки TextMeshProUGUI:**
   ```
   Text: (оставьте пустым)
   Font Asset: Roboto-Bold
   Font Size: 20
   Color: R: 204, G: 204, B: 204, A: 230 (светло-серый)
   Alignment: Bottom Left
   Wrapping: Enabled ✓
   Overflow: Overflow
   ```

3. **Rect Transform для ActionHintText:**
   ```
   Anchors: Min (0, 0), Max (1, 0.25)
   Left: 15
   Top: 15
   Right: -15
   Bottom: -5
   Pivot: (0, 0)
   ```

---

### ШАГ 3: Добавление компонента ServerControllerWindow

1. **Выберите объект `ServerWindow`**
2. **Добавьте компонент `ServerControllerWindow`**
3. **Настройте все ссылки:**

   ```
   UI References:
   - Window Canvas: перетащите ServerCanvas
   - Server Name Text: перетащите ServerNameText
   - Server Status Text: перетащите ServerStatusText
   - Server Details Text: перетащите ServerDetailsText
   - Action Hint Text: перетащите ActionHintText
   - Window Panel: перетащите WindowPanel
   
   Window Settings:
   - Window Distance: 0.6
   - Window Size: X: 0.55, Y: 0.75
   - Window Height: 0.1
   - Font Size: 24
   
   Server Settings:
   - Server Index: должен совпадать с ServerController
     * 0 для servers-1
     * 1 для servers-2
     * 2 для servers-3
     * 3 для servers-3A
   - Server Display Name: должен совпадать с ServerController
     * "Сервер 1" для servers-1
     * "Сервер 2" для servers-2
     * "Сервер 3" для servers-3
     * "Сервер 4" для servers-3A
   ```

---

### ШАГ 4: Связывание ServerController с ServerControllerWindow

1. **Вернитесь к основному объекту сервера** (например, `servers-1`)
2. **В компоненте `ServerController`:**
   - **Server Window:** перетащите объект `ServerWindow`

---

### ШАГ 5: Проверка XR Interaction Manager

1. **Убедитесь, что в сцене есть `XR Interaction Manager`:**
   - Если нет - создайте пустой GameObject
   - Добавьте компонент `XR Interaction Manager`

---

## 📐 ТОЧНЫЕ РАЗМЕРЫ И ПОЗИЦИИ (СВОДКА)

### ServerCanvas:
- **Width:** 550
- **Height:** 750
- **Scale:** (0.001, 0.001, 0.001)
- **Position:** (0, 0, 0) относительно ServerWindow

### WindowPanel:
- **Anchors:** Stretch-Stretch (0,0 до 1,1)
- **Offsets:** все 0

### ServerNameText:
- **Width:** 500
- **Height:** 50
- **Anchors:** (0.5, 0.85) до (0.5, 1.0)
- **Pivot:** (0.5, 1.0)
- **Position:** (0, 0)

### ServerStatusText:
- **Anchors:** (0, 0.7) до (1, 0.85)
- **Offsets:** Left: 15, Right: -15, Top: 0, Bottom: -5

### ServerDetailsText:
- **Anchors:** (0, 0.25) до (1, 0.7)
- **Offsets:** Left: 15, Right: -15, Top: 15, Bottom: -5

### ActionHintText:
- **Anchors:** (0, 0) до (1, 0.25)
- **Offsets:** Left: 15, Right: -15, Top: 15, Bottom: -5

---

## 🎮 КАК ЭТО РАБОТАЕТ

1. **Короткое нажатие на сервер:**
   - Окно открывается перед игроком
   - Отображается информация о сервере

2. **Длительное нажатие (удерживайте ≥1 секунду):**
   - Окно закрывается

3. **Визуальная обратная связь:**
   - При наведении сервер подсвечивается (если настроен Hover Material)

---

## ⚠️ ВАЖНЫЕ МОМЕНТЫ

1. **Server Index должен быть уникальным** для каждого сервера (0-3)
2. **Server Display Name должен совпадать** в обоих компонентах
3. **Все ссылки должны быть заполнены** в ServerControllerWindow
4. **XR Interaction Manager должен быть в сцене**
5. **MonitoringDataService должен быть в сцене** (для данных о серверах)

---

## 🔧 РЕШЕНИЕ ПРОБЛЕМ

### Окно не открывается:
- Проверьте, что `Server Window` заполнен в `ServerController`
- Проверьте, что у сервера есть `XRSimpleInteractable` или `XRBaseInteractable`
- Проверьте наличие `XR Interaction Manager` в сцене

### Окно открывается не там:
- Проверьте `Window Distance` и `Window Height` в `ServerControllerWindow`
- Проверьте, что камера найдена (должна быть Main Camera или активная камера)

### Текст не виден:
- Проверьте размеры Rect Transform для каждого текстового элемента
- Проверьте цвет текста (должен быть белый или светлый)
- Проверьте, что Font Asset назначен

### Длительное нажатие не работает:
- Проверьте `Long Press Threshold` в `ServerController` (должно быть 1.0)
- Убедитесь, что удерживаете нажатие достаточно долго (≥1 секунда)

---

## ✅ ГОТОВО!

После выполнения всех шагов система должна работать точно так же, как у камер наблюдения:
- Короткое нажатие → открыть
- Длительное нажатие → закрыть
