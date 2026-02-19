using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Контроллер просмотра PDF в VR
/// Примечание: Runtime PDF рендеринг требует сторонних библиотек
/// Для MVP можно использовать предконвертированные текстуры страниц
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class PdfViewerController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas pdfCanvas;
    [SerializeField] private RawImage pdfDisplayImage;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI pageNumberText;
    [SerializeField] private GameObject pdfPanel;

    [Header("PDF Settings")]
    [SerializeField] private Texture2D[] pdfPages; // Предконвертированные страницы PDF
    [SerializeField] private string pdfTitle = "Регламент";

    private XRSimpleInteractable interactable;
    private int currentPageIndex = 0;
    private bool isViewerOpen = false;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        if (pdfCanvas == null)
        {
            pdfCanvas = GetComponentInChildren<Canvas>();
        }

        if (pdfPanel != null)
        {
            pdfPanel.SetActive(false);
        }
    }

    private void Start()
    {
        SetupButtons();
        UpdatePageDisplay();
    }

    private void SetupButtons()
    {
        // Настройка кнопок для XR взаимодействия
        if (nextPageButton != null)
        {
            var nextInteractable = nextPageButton.GetComponent<XRSimpleInteractable>();
            if (nextInteractable == null)
            {
                nextInteractable = nextPageButton.gameObject.AddComponent<XRSimpleInteractable>();
            }
            nextPageButton.onClick.AddListener(NextPage);
        }

        if (previousPageButton != null)
        {
            var prevInteractable = previousPageButton.GetComponent<XRSimpleInteractable>();
            if (prevInteractable == null)
            {
                prevInteractable = previousPageButton.gameObject.AddComponent<XRSimpleInteractable>();
            }
            previousPageButton.onClick.AddListener(PreviousPage);
        }

        if (closeButton != null)
        {
            var closeInteractable = closeButton.GetComponent<XRSimpleInteractable>();
            if (closeInteractable == null)
            {
                closeInteractable = closeButton.gameObject.AddComponent<XRSimpleInteractable>();
            }
            closeButton.onClick.AddListener(CloseViewer);
        }

        // Активация через XR Interaction
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnPdfActivated);
        }
    }

    private void OnDestroy()
    {
        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveAllListeners();
        }

        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveAllListeners();
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
        }

        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnPdfActivated);
        }
    }

    private void OnPdfActivated(SelectEnterEventArgs args)
    {
        OpenViewer();
    }

    private void OpenViewer()
    {
        if (pdfPanel != null)
        {
            pdfPanel.SetActive(true);
        }
        isViewerOpen = true;
        UpdatePageDisplay();
    }

    private void CloseViewer()
    {
        if (pdfPanel != null)
        {
            pdfPanel.SetActive(false);
        }
        isViewerOpen = false;
    }

    private void NextPage()
    {
        if (pdfPages == null || pdfPages.Length == 0)
            return;

        currentPageIndex = Mathf.Min(currentPageIndex + 1, pdfPages.Length - 1);
        UpdatePageDisplay();
    }

    private void PreviousPage()
    {
        if (pdfPages == null || pdfPages.Length == 0)
            return;

        currentPageIndex = Mathf.Max(currentPageIndex - 1, 0);
        UpdatePageDisplay();
    }

    private void UpdatePageDisplay()
    {
        if (pdfPages == null || pdfPages.Length == 0)
        {
            if (pdfDisplayImage != null)
            {
                pdfDisplayImage.texture = null;
            }
            if (pageNumberText != null)
            {
                pageNumberText.text = "PDF не загружен";
            }
            return;
        }

        // Обновление изображения страницы
        if (pdfDisplayImage != null && currentPageIndex >= 0 && currentPageIndex < pdfPages.Length)
        {
            pdfDisplayImage.texture = pdfPages[currentPageIndex];
        }

        // Обновление номера страницы
        if (pageNumberText != null)
        {
            pageNumberText.text = $"{pdfTitle}\nСтраница {currentPageIndex + 1} / {pdfPages.Length}";
        }

        // Обновление состояния кнопок
        if (previousPageButton != null)
        {
            previousPageButton.interactable = currentPageIndex > 0;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = currentPageIndex < pdfPages.Length - 1;
        }
    }

    // Публичные методы для программного управления
    public void LoadPdfPages(Texture2D[] pages)
    {
        pdfPages = pages;
        currentPageIndex = 0;
        UpdatePageDisplay();
    }

    public void SetPdfTitle(string title)
    {
        pdfTitle = title;
        UpdatePageDisplay();
    }

    public bool IsViewerOpen()
    {
        return isViewerOpen;
    }
}