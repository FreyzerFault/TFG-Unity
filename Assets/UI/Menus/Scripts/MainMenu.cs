using UnityEngine;

public class MainMenu : MenuManager
{
    private static readonly int OpenID = Animator.StringToHash("open");

    protected override void Awake()
    {
        base.Awake();
        
        rootMenu.Open();
    }

    public void Play()
    {
        if (Close())
            LevelManager.Instance.LoadSelectedLevel();
    }

    public void Quit()
    {
        if (Close())
            GameManager.Quit();
    }

    public override void OnCloseMenu() { }
}
