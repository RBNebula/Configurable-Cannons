using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MineMogul.ConveyorCannonTweaks
{
    internal sealed class CannonTweaksAssetBundleUi : IDisposable
    {
        private readonly Action<CannonTweaksSettingsSnapshot> _applySettings;
        private readonly ManualLogSource _logger;

        private GameObject _canvasObject;
        private GameObject _overlayObject;
        private AssetBundle _uiBundle;
        private Font _font;
        private static Sprite _solidSprite;

        private NumericControl _pushXControl;
        private NumericControl _pushYControl;
        private NumericControl _pushZControl;

        private bool _isOpen;
        private bool _isSyncing;
        private bool _hasCapturedCursorState;
        private bool _previousCursorVisible;
        private CursorLockMode _previousCursorLockMode;
        private const float CompactWindowHeight = 470f;
        private const float CompactSettingsCardHeight = 250f;
        private const float PushVelocityMin = -100f;
        private const float PushVelocityMax = 100f;

        private readonly List<MonoBehaviour> _disabledPlayerScripts = new List<MonoBehaviour>();
        private MonoBehaviour _suppressedUiManagerBehaviour;
        private bool _suppressedUiManagerWasEnabled;

        internal bool IsOpen
        {
            get { return _isOpen; }
        }

        internal CannonTweaksAssetBundleUi(Action<CannonTweaksSettingsSnapshot> applySettings, ManualLogSource logger)
        {
            _applySettings = applySettings;
            _logger = logger;
        }

        public void Dispose()
        {
            if (_isOpen)
            {
                Close();
            }

            if (_canvasObject != null)
            {
                UnityEngine.Object.Destroy(_canvasObject);
                _canvasObject = null;
            }

            if (_uiBundle != null)
            {
                _uiBundle.Unload(false);
                _uiBundle = null;
            }
        }

        internal void Open(CannonTweaksSettingsSnapshot snapshot)
        {
            EnsureUiCreated();
            if (_overlayObject == null)
            {
                return;
            }

            ApplySnapshotToControls(snapshot);
            _overlayObject.SetActive(true);
            _isOpen = true;

            EnsureEventSystemExists();
            CaptureCursorState();
            DisableGameplayControls();
            ShowCursor();
        }

        internal void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            if (_overlayObject != null)
            {
                _overlayObject.SetActive(false);
            }

            _isOpen = false;
            RestoreGameplayControls();
            RestoreCursorState();

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        internal void TickWhileOpen()
        {
            if (!_isOpen)
            {
                return;
            }

            ShowCursor();

            if (_disabledPlayerScripts.Count == 0)
            {
                DisableGameplayControls();
            }

            if (_suppressedUiManagerBehaviour != null)
            {
                _suppressedUiManagerBehaviour.enabled = false;
            }
        }

        private void EnsureUiCreated()
        {
            if (_canvasObject != null && _overlayObject != null)
            {
                return;
            }

            _canvasObject = new GameObject(
                "ConveyorCannonTweaksCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(_canvasObject);

            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6000;

            CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _overlayObject = TryCreateOverlayFromBundle(_canvasObject.transform);
            if (_overlayObject != null)
            {
                _overlayObject.SetActive(false);
                _logger.LogInfo("Loaded ConfigurableCannons UI from asset bundle.");
                return;
            }

            EnsureSolidSprite();
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            RectTransform overlay = CreateImageRect(_canvasObject.transform, "Overlay", new Color(0.01f, 0.05f, 0.12f, 0.80f));
            StretchToParent(overlay, 0f);
            _overlayObject = overlay.gameObject;

            RectTransform panel = CreateImageRect(overlay, "Window", new Color(0.05f, 0.14f, 0.25f, 0.97f));
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1060f, CompactWindowHeight);
            panel.anchoredPosition = Vector2.zero;

            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.37f, 0.77f, 1f, 0.55f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            Shadow panelShadow = panel.gameObject.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            panelShadow.effectDistance = new Vector2(0f, -5f);

            RectTransform accentBar = CreateImageRect(panel, "AccentBar", new Color(0.30f, 0.80f, 1f, 0.95f));
            accentBar.anchorMin = new Vector2(0f, 1f);
            accentBar.anchorMax = new Vector2(1f, 1f);
            accentBar.pivot = new Vector2(0.5f, 1f);
            accentBar.sizeDelta = new Vector2(0f, 6f);
            accentBar.anchoredPosition = Vector2.zero;

            RectTransform content = CreateRectOnly(panel, "Content");
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(24f, 22f);
            content.offsetMax = new Vector2(-24f, -24f);

            VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 12f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            BuildHeader(content);
            BuildSettingsCard(content);
            BuildFooter(content);

            _overlayObject.SetActive(false);
            _logger.LogInfo("Built in-code ConfigurableCannons UI fallback.");
        }

        private GameObject TryCreateOverlayFromBundle(Transform parent)
        {
            foreach (string path in GetBundleCandidatePaths())
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                AssetBundle bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null)
                {
                    _logger.LogWarning("Could not load UI bundle file: " + path);
                    continue;
                }

                GameObject panel = TryInstantiateAndBindPanel(bundle, parent);
                if (panel != null)
                {
                    _uiBundle = bundle;
                    _logger.LogInfo("Loaded Cannon UI bundle from: " + path);
                    return panel;
                }

                bundle.Unload(false);
                _logger.LogWarning("UI bundle found but panel binding failed: " + path);
            }

            return null;
        }

        private IEnumerable<string> GetBundleCandidatePaths()
        {
            string pluginRoot = Paths.PluginPath;
            string pluginFolder = Path.Combine(pluginRoot, "ConveyorCannonTweaks");
            string bundleName = ModInfo.UiBundleFileName;

            yield return Path.Combine(pluginFolder, bundleName);
            yield return Path.Combine(pluginFolder, bundleName + ".bundle");
            yield return Path.Combine(pluginRoot, bundleName);
            yield return Path.Combine(pluginRoot, bundleName + ".bundle");
            yield return ModInfo.UiBundleDevPath;
            yield return ModInfo.UiBundleDevPath + ".bundle";
            yield return ModInfo.UiPreviewBundleBuildPath;
            yield return ModInfo.UiPreviewBundleBuildPath + ".bundle";
        }

        private GameObject TryInstantiateAndBindPanel(AssetBundle bundle, Transform parent)
        {
            string[] assetNames = bundle.GetAllAssetNames();
            for (int i = 0; i < assetNames.Length; i++)
            {
                GameObject prefab = bundle.LoadAsset<GameObject>(assetNames[i]);
                if (prefab == null)
                {
                    continue;
                }

                GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);
                string bindError;
                if (TryBindPanelControls(instance.transform, out bindError))
                {
                    instance.name = "ConveyorCannonTweaksPanel";
                    return instance;
                }

                UnityEngine.Object.Destroy(instance);
                _logger.LogWarning("UI prefab candidate rejected: " + assetNames[i] + " | " + bindError);
            }

            return null;
        }

        private bool TryBindPanelControls(Transform panelRoot, out string bindError)
        {
            Transform beltSpeedRow = FindTransformRecursive(panelRoot, "Row_BeltSpeed");
            if (beltSpeedRow != null)
            {
                beltSpeedRow.gameObject.SetActive(false);
            }

            _pushXControl = TryBindNumericControl(panelRoot, "Row_PushX", PushVelocityMin, PushVelocityMax);
            _pushYControl = TryBindNumericControl(panelRoot, "Row_PushY", PushVelocityMin, PushVelocityMax);
            _pushZControl = TryBindNumericControl(panelRoot, "Row_PushZ", PushVelocityMin, PushVelocityMax);
            Transform retainRow = FindTransformRecursive(panelRoot, "Row_RetainY");
            if (retainRow != null)
            {
                retainRow.gameObject.SetActive(false);
            }

            CompactBoundPanelLayout(panelRoot);

            Button setButton = FindButton(panelRoot, "set") ?? FindButton(panelRoot, "apply");
            Button closeButton = FindButton(panelRoot, "close") ?? FindButton(panelRoot, "exit") ?? FindButton(panelRoot, "cancel");

            List<string> missing = new List<string>();
            if (_pushXControl == null) missing.Add("Row_PushX");
            if (_pushYControl == null) missing.Add("Row_PushY");
            if (_pushZControl == null) missing.Add("Row_PushZ");
            if (setButton == null) missing.Add("Set/Apply Button");
            if (closeButton == null) missing.Add("Close/Exit/Cancel Button");

            if (missing.Count > 0)
            {
                bindError = "Missing controls: " + string.Join(", ", missing);
                return false;
            }

            setButton.onClick.AddListener(OnSetClicked);
            closeButton.onClick.AddListener(Close);
            bindError = string.Empty;
            return true;
        }

        private NumericControl TryBindNumericControl(Transform panelRoot, string rowName, float min, float max)
        {
            Transform row = FindTransformRecursive(panelRoot, rowName);
            if (row == null)
            {
                return null;
            }

            Slider slider = row.GetComponentInChildren<Slider>(true);
            InputField input = row.GetComponentInChildren<InputField>(true);
            if (slider == null || input == null)
            {
                return null;
            }

            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;

            NumericControl control = new NumericControl
            {
                Slider = slider,
                Input = input,
                Min = min,
                Max = max
            };

            slider.onValueChanged.AddListener(value => OnSliderValueChanged(control, value));
            input.onEndEdit.AddListener(_ => SyncInputToSlider(control));
            return control;
        }

        private static Button FindButton(Transform root, string token)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                if (button.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return button;
                }

                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null && label.text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return button;
                }
            }

            return null;
        }

        private static Transform FindTransformRecursive(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.name, name, StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindTransformRecursive(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
        private void BuildHeader(Transform parent)
        {
            RectTransform row = CreateRectOnly(parent, "Header");
            LayoutElement rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 92f;

            VerticalLayoutGroup headerLayout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            headerLayout.spacing = 4f;
            headerLayout.padding = new RectOffset(8, 8, 4, 4);
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            CreateText(
                row,
                "Configurable Cannons",
                34,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.95f, 0.98f, 1f, 1f),
                preferredHeight: 48f);

            CreateText(
                row,
                "Tune cannon push velocity. Use Set to apply instantly.",
                15,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.85f, 0.93f, 1f, 0.95f),
                preferredHeight: 28f);
        }

        private void BuildSettingsCard(Transform parent)
        {
            RectTransform card = CreateImageRect(parent, "SettingsCard", new Color(0.07f, 0.18f, 0.31f, 0.96f));
            LayoutElement cardElement = card.gameObject.AddComponent<LayoutElement>();
            cardElement.flexibleHeight = 0f;
            cardElement.minHeight = CompactSettingsCardHeight;
            cardElement.preferredHeight = CompactSettingsCardHeight;

            Outline cardOutline = card.gameObject.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.24f, 0.62f, 0.92f, 0.35f);
            cardOutline.effectDistance = new Vector2(1f, -1f);

            VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _pushXControl = CreateNumericRow(card, "Cannon Push Velocity x", PushVelocityMin, PushVelocityMax);
            _pushYControl = CreateNumericRow(card, "Cannon Push Velocity y", PushVelocityMin, PushVelocityMax);
            _pushZControl = CreateNumericRow(card, "Cannon Push Velocity z", PushVelocityMin, PushVelocityMax);
        }

        private void CompactBoundPanelLayout(Transform panelRoot)
        {
            RectTransform window = FindTransformRecursive(panelRoot, "Window") as RectTransform;
            if (window == null)
            {
                window = panelRoot as RectTransform;
            }

            if (window != null)
            {
                window.sizeDelta = new Vector2(window.sizeDelta.x, CompactWindowHeight);
            }

            Transform settingsCard = FindTransformRecursive(panelRoot, "SettingsCard");
            if (settingsCard != null)
            {
                LayoutElement settingsLayout = settingsCard.GetComponent<LayoutElement>();
                if (settingsLayout != null)
                {
                    settingsLayout.flexibleHeight = 0f;
                    settingsLayout.minHeight = CompactSettingsCardHeight;
                    settingsLayout.preferredHeight = CompactSettingsCardHeight;
                }

                RectTransform settingsRect = settingsCard as RectTransform;
                if (settingsRect != null)
                {
                    settingsRect.sizeDelta = new Vector2(settingsRect.sizeDelta.x, CompactSettingsCardHeight);
                }
            }
        }

        private void CreateSectionTitle(Transform parent, string text)
        {
            RectTransform row = CreateRectOnly(parent, text + "Header");
            LayoutElement rowElement = row.gameObject.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 28f;

            CreateText(
                row,
                text,
                17,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.53f, 0.84f, 1f, 1f),
                preferredHeight: 26f);
        }

        private void BuildFooter(Transform parent)
        {
            RectTransform row = CreateRectOnly(parent, "Footer");
            LayoutElement rowElement = row.gameObject.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 56f;

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(row, false);
            LayoutElement spacerElement = spacer.GetComponent<LayoutElement>();
            spacerElement.flexibleWidth = 1f;

            CreateActionButton(
                row,
                "Set",
                OnSetClicked,
                new Color(0.17f, 0.54f, 0.86f, 0.98f),
                new Color(0.26f, 0.66f, 0.95f, 1f),
                150f,
                46f);

            CreateActionButton(
                row,
                "Close",
                Close,
                new Color(0.10f, 0.29f, 0.49f, 0.98f),
                new Color(0.18f, 0.40f, 0.63f, 1f),
                150f,
                46f);
        }

        private NumericControl CreateNumericRow(Transform parent, string label, float min, float max)
        {
            RectTransform row = CreateImageRect(parent, label.Replace(" ", string.Empty) + "Row", new Color(0.09f, 0.23f, 0.38f, 0.98f));
            LayoutElement rowElement = row.gameObject.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 54f;

            HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.padding = new RectOffset(12, 12, 7, 7);
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            Text labelText = CreateText(
                row,
                label,
                18,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.97f, 0.99f, 1f, 1f),
                preferredHeight: 34f);
            LayoutElement labelElement = labelText.gameObject.GetComponent<LayoutElement>();
            labelElement.preferredWidth = 300f;
            labelElement.minWidth = 300f;

            GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGo.name = label.Replace(" ", string.Empty) + "Slider";
            sliderGo.transform.SetParent(row, false);
            LayoutElement sliderLayout = sliderGo.AddComponent<LayoutElement>();
            sliderLayout.flexibleWidth = 1f;
            sliderLayout.minWidth = 260f;

            Slider slider = sliderGo.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;

            Image background = sliderGo.transform.Find("Background") != null
                ? sliderGo.transform.Find("Background").GetComponent<Image>()
                : null;
            if (background != null)
            {
                background.sprite = _solidSprite;
                background.type = Image.Type.Sliced;
                background.color = new Color(0.46f, 0.49f, 0.54f, 1f);
            }

            Image fill = sliderGo.transform.Find("Fill Area/Fill") != null
                ? sliderGo.transform.Find("Fill Area/Fill").GetComponent<Image>()
                : null;
            if (fill != null)
            {
                fill.sprite = _solidSprite;
                fill.type = Image.Type.Sliced;
                fill.color = new Color(0.37f, 0.87f, 1f, 1f);
            }

            Image handle = sliderGo.transform.Find("Handle Slide Area/Handle") != null
                ? sliderGo.transform.Find("Handle Slide Area/Handle").GetComponent<Image>()
                : null;
            if (handle != null)
            {
                handle.sprite = _solidSprite;
                handle.type = Image.Type.Sliced;
                handle.color = Color.white;
                RectTransform handleRect = handle.GetComponent<RectTransform>();
                if (handleRect != null)
                {
                    handleRect.sizeDelta = new Vector2(26f, 34f);
                }

                Outline handleOutline = handle.gameObject.GetComponent<Outline>();
                if (handleOutline == null)
                {
                    handleOutline = handle.gameObject.AddComponent<Outline>();
                }

                handleOutline.effectColor = new Color(0.08f, 0.15f, 0.24f, 0.95f);
                handleOutline.effectDistance = new Vector2(1f, -1f);
            }

            slider.targetGraphic = handle;

            InputField input = CreateNumberInput(row, 128f);

            NumericControl control = new NumericControl
            {
                Slider = slider,
                Input = input,
                Min = min,
                Max = max
            };

            slider.onValueChanged.AddListener(value => OnSliderValueChanged(control, value));
            input.onEndEdit.AddListener(_ => SyncInputToSlider(control));
            return control;
        }

        private InputField CreateNumberInput(Transform parent, float width)
        {
            GameObject inputGo = DefaultControls.CreateInputField(new DefaultControls.Resources());
            inputGo.name = "ValueInput";
            inputGo.transform.SetParent(parent, false);

            LayoutElement layoutElement = inputGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.minWidth = width;
            layoutElement.preferredHeight = 40f;

            InputField input = inputGo.GetComponent<InputField>();
            input.contentType = InputField.ContentType.DecimalNumber;
            input.lineType = InputField.LineType.SingleLine;
            input.caretWidth = 2;
            input.caretColor = new Color(0.95f, 0.99f, 1f, 1f);
            input.selectionColor = new Color(0.32f, 0.66f, 0.95f, 0.42f);

            Image bg = inputGo.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = _solidSprite;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.12f, 0.25f, 0.41f, 0.98f);
            }

            Transform textTransform = inputGo.transform.Find("Text");
            if (textTransform == null)
            {
                textTransform = inputGo.transform.Find("Text Area/Text");
            }

            Text text = textTransform != null
                ? textTransform.GetComponent<Text>()
                : input.textComponent;
            if (text != null)
            {
                text.font = _font;
                text.fontSize = 16;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.supportRichText = false;
                input.textComponent = text;
            }

            Transform placeholderTransform = inputGo.transform.Find("Placeholder");
            if (placeholderTransform == null)
            {
                placeholderTransform = inputGo.transform.Find("Text Area/Placeholder");
            }

            Text placeholder = placeholderTransform != null
                ? placeholderTransform.GetComponent<Text>()
                : input.placeholder as Text;
            if (placeholder != null)
            {
                placeholder.font = _font;
                placeholder.fontSize = 16;
                placeholder.alignment = TextAnchor.MiddleCenter;
                placeholder.color = new Color(0.74f, 0.84f, 0.94f, 0.70f);
                placeholder.text = "0";
                placeholder.supportRichText = false;
                input.placeholder = placeholder;
            }

            return input;
        }

        private Button CreateActionButton(
            Transform parent,
            string text,
            Action onClick,
            Color normalColor,
            Color highlightedColor,
            float width,
            float height)
        {
            GameObject buttonGo = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonGo.name = text + "Button";
            buttonGo.transform.SetParent(parent, false);

            LayoutElement layoutElement = buttonGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.minWidth = width;
            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;

            Button button = buttonGo.GetComponent<Button>();
            Image image = buttonGo.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = _solidSprite;
                image.type = Image.Type.Sliced;
                image.color = normalColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = new Color(
                Mathf.Clamp01(normalColor.r - 0.06f),
                Mathf.Clamp01(normalColor.g - 0.08f),
                Mathf.Clamp01(normalColor.b - 0.10f),
                normalColor.a);
            colors.selectedColor = highlightedColor;
            colors.disabledColor = new Color(0.28f, 0.36f, 0.46f, 0.9f);
            button.colors = colors;

            Text label = buttonGo.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
                label.font = _font;
                label.fontSize = 20;
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleCenter;
            }

            button.onClick.AddListener(() => onClick());
            return button;
        }

        private RectTransform CreateRectOnly(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private RectTransform CreateImageRect(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = _solidSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return go.GetComponent<RectTransform>();
        }

        private Text CreateText(
            Transform parent,
            string text,
            int fontSize,
            FontStyle style,
            TextAnchor anchor,
            Color color,
            float preferredHeight = 0f)
        {
            GameObject go = new GameObject(text.Replace(" ", string.Empty) + "Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text uiText = go.GetComponent<Text>();
            uiText.text = text;
            uiText.font = _font;
            uiText.fontSize = fontSize;
            uiText.fontStyle = style;
            uiText.alignment = anchor;
            uiText.color = color;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            uiText.supportRichText = false;

            LayoutElement layoutElement = go.AddComponent<LayoutElement>();
            if (preferredHeight > 0f)
            {
                layoutElement.preferredHeight = preferredHeight;
            }

            return uiText;
        }

        private static void StretchToParent(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void EnsureSolidSprite()
        {
            if (_solidSprite != null)
            {
                return;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.name = "ConveyorCannonTweaks_UiSolid";
            tex.SetPixel(0, 0, Color.white);
            tex.SetPixel(1, 0, Color.white);
            tex.SetPixel(0, 1, Color.white);
            tex.SetPixel(1, 1, Color.white);
            tex.Apply(false, true);

            _solidSprite = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
        }

        private void OnSliderValueChanged(NumericControl control, float value)
        {
            if (_isSyncing)
            {
                return;
            }

            _isSyncing = true;
            control.Input.text = value.ToString("0.###", CultureInfo.InvariantCulture);
            _isSyncing = false;
        }

        private void SyncInputToSlider(NumericControl control)
        {
            if (_isSyncing)
            {
                return;
            }

            float parsed;
            if (!float.TryParse(control.Input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                _isSyncing = true;
                control.Input.text = control.Slider.value.ToString("0.###", CultureInfo.InvariantCulture);
                _isSyncing = false;
                return;
            }

            parsed = Mathf.Clamp(parsed, control.Min, control.Max);

            _isSyncing = true;
            control.Slider.value = parsed;
            control.Input.text = parsed.ToString("0.###", CultureInfo.InvariantCulture);
            _isSyncing = false;
        }

        private void ApplySnapshotToControls(CannonTweaksSettingsSnapshot snapshot)
        {
            if (_pushXControl == null || _pushYControl == null || _pushZControl == null)
            {
                return;
            }

            _isSyncing = true;
            SetControlValue(_pushXControl, snapshot.PushVelocityX);
            SetControlValue(_pushYControl, snapshot.PushVelocityY);
            SetControlValue(_pushZControl, snapshot.PushVelocityZ);
            _isSyncing = false;
        }

        private static void SetControlValue(NumericControl control, float value)
        {
            float clamped = Mathf.Clamp(value, control.Min, control.Max);
            control.Slider.value = clamped;
            control.Input.text = clamped.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private void OnSetClicked()
        {
            if (_pushXControl == null || _pushYControl == null || _pushZControl == null)
            {
                return;
            }

            SyncInputToSlider(_pushXControl);
            SyncInputToSlider(_pushYControl);
            SyncInputToSlider(_pushZControl);

            CannonTweaksSettingsSnapshot snapshot = new CannonTweaksSettingsSnapshot
            {
                PushVelocityX = _pushXControl.Slider.value,
                PushVelocityY = _pushYControl.Slider.value,
                PushVelocityZ = _pushZControl.Slider.value
            };

            _applySettings(snapshot);
        }

        private static void EnsureEventSystemExists()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("ConveyorCannonTweaksEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(eventSystemObject);
        }

        private void CaptureCursorState()
        {
            if (_hasCapturedCursorState)
            {
                return;
            }

            _previousCursorVisible = Cursor.visible;
            _previousCursorLockMode = Cursor.lockState;
            _hasCapturedCursorState = true;
        }

        private void DisableGameplayControls()
        {
            GameObject playerRoot = GameObject.FindWithTag("Player");
            if (playerRoot == null && Camera.main != null)
            {
                playerRoot = Camera.main.transform.root.gameObject;
            }

            if (playerRoot != null)
            {
                MonoBehaviour[] components = playerRoot.GetComponents<MonoBehaviour>();
                for (int i = 0; i < components.Length; i++)
                {
                    MonoBehaviour mono = components[i];
                    if (mono == null)
                    {
                        continue;
                    }

                    string typeName = mono.GetType().Name;
                    if (typeName != "PlayerController" && typeName != "PlayerInventory")
                    {
                        continue;
                    }

                    if (!mono.enabled)
                    {
                        continue;
                    }

                    mono.enabled = false;
                    _disabledPlayerScripts.Add(mono);
                }
            }

            if (_suppressedUiManagerBehaviour == null)
            {
                MonoBehaviour uiManager = Singleton<UIManager>.Instance as MonoBehaviour;
                if (uiManager != null)
                {
                    _suppressedUiManagerBehaviour = uiManager;
                    _suppressedUiManagerWasEnabled = uiManager.enabled;
                    uiManager.enabled = false;
                }
            }
        }

        private static void ShowCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void RestoreCursorState()
        {
            if (!_hasCapturedCursorState)
            {
                return;
            }

            Cursor.visible = _previousCursorVisible;
            Cursor.lockState = _previousCursorLockMode;
            _hasCapturedCursorState = false;
        }

        private void RestoreGameplayControls()
        {
            for (int i = 0; i < _disabledPlayerScripts.Count; i++)
            {
                MonoBehaviour mono = _disabledPlayerScripts[i];
                if (mono != null)
                {
                    mono.enabled = true;
                }
            }

            _disabledPlayerScripts.Clear();

            if (_suppressedUiManagerBehaviour != null)
            {
                _suppressedUiManagerBehaviour.enabled = _suppressedUiManagerWasEnabled;
                _suppressedUiManagerBehaviour = null;
                _suppressedUiManagerWasEnabled = false;
            }
        }

        private sealed class NumericControl
        {
            public Slider Slider;
            public InputField Input;
            public float Min;
            public float Max;
        }
    }
}
