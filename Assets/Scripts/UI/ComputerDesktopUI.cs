using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Desktop-style UI: opens messenger or deliveries panels from icon buttons;
/// each panel has a close (X) control that hides only that panel.
/// </summary>
public class ComputerDesktopUI : MonoBehaviour
{
    [Header("Icon buttons")]
    [SerializeField] private Button messengerIconButton;
    [SerializeField] private Button deliveriesIconButton;

    [Header("Panels")]
    [SerializeField] private GameObject messengerPanel;
    [SerializeField] private GameObject deliveriesPanel;

    [Header("Close (X) buttons")]
    [SerializeField] private Button messengerCloseButton;
    [SerializeField] private Button deliveriesCloseButton;

    private void Awake()
    {
        if (messengerPanel != null)
            messengerPanel.SetActive(false);
        if (deliveriesPanel != null)
            deliveriesPanel.SetActive(false);

        if (messengerIconButton != null)
            messengerIconButton.onClick.AddListener(OpenMessengerPanel);
        if (deliveriesIconButton != null)
            deliveriesIconButton.onClick.AddListener(OpenDeliveriesPanel);

        if (messengerCloseButton != null)
            messengerCloseButton.onClick.AddListener(CloseMessengerPanel);
        if (deliveriesCloseButton != null)
            deliveriesCloseButton.onClick.AddListener(CloseDeliveriesPanel);
    }

    private void OnDestroy()
    {
        if (messengerIconButton != null)
            messengerIconButton.onClick.RemoveListener(OpenMessengerPanel);
        if (deliveriesIconButton != null)
            deliveriesIconButton.onClick.RemoveListener(OpenDeliveriesPanel);
        if (messengerCloseButton != null)
            messengerCloseButton.onClick.RemoveListener(CloseMessengerPanel);
        if (deliveriesCloseButton != null)
            deliveriesCloseButton.onClick.RemoveListener(CloseDeliveriesPanel);
    }

    public void OpenMessengerPanel()
    {
        if (messengerPanel != null)
            messengerPanel.SetActive(true);
    }

    public void CloseMessengerPanel()
    {
        if (messengerPanel != null)
            messengerPanel.SetActive(false);
    }

    public void OpenDeliveriesPanel()
    {
        if (deliveriesPanel != null)
            deliveriesPanel.SetActive(true);
    }

    public void CloseDeliveriesPanel()
    {
        if (deliveriesPanel != null)
            deliveriesPanel.SetActive(false);
    }
}
