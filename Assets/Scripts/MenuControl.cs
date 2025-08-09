using System.Collections.Generic;
using MixedReality.Toolkit.UX;
using UnityEngine;
using Random = UnityEngine.Random;

public class MenuControl : MonoBehaviour
{
    [SerializeField] private GameObject notificationControl;
    [SerializeField] private GameObject audioControl;
    [SerializeField] private GameObject gazeControl;
    public DialogPool dialogPool;
    [SerializeField] private GameObject menuDialog;

    private void Start()
    {
        ShowMenuDialog();
    }

    private void ShowMenuDialog()
    {
        var dialog = dialogPool.Get(prefab: menuDialog);

        dialog.SetHeader("Welcome!")
              .SetBody("This experiment explores Head-mounted displays (HMDs) " +
                       "in the physical world by observing participants as they " +
                       "cycle along a fixed outdoor track. At the same time, notifications" +
                       "positioned at variable locations and of varying complexity are presented, " +
                       "aiming to examine how both spatial positioning and visual complexity " +
                       "influence cyclists' attention. The experiment will start right after this." +
                       "Be ready!")
              .SetPositive("Start", _ =>
                                    {
                                        dialog.Dismiss();

                                        Debug.Log("Starting experiment!");
                                        StartCaseStudy();
                                        StartEyeTracking();
                                    })
              .SetNegative("Quit", _ =>
                                   {
                                       dialog.Dismiss();

                                       Debug.Log("End experiment!");
                                       QuitApplication();
                                   })
              .Show();
    }

    private static void QuitApplication()
    {
        Application.Quit();
    }

    private static void SetGameObjectActive(GameObject target, bool state)
    {
        target.SetActive(state);
    }

    private void StartEyeTracking()
    {
        SetGameObjectActive(gazeControl, true);
    }

    private void StartCaseStudy()
    {
        SetGameObjectActive(notificationControl, true);
        SetGameObjectActive(audioControl, true);
    }
}