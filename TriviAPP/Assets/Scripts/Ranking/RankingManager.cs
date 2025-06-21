using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RankingManager : MonoBehaviour
{
    public static RankingManager Instance { get; private set; }

    [Header("Referencias")]
    public Transform contenedorRanking;
    public GameObject rankingEntryPrefab;
    public AvatarManager avatarManager;
    public List<Sprite> medallasTop3;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        BuscarReferenciasSiFaltan();
    }

    private void BuscarReferenciasSiFaltan()
    {
        if (contenedorRanking == null)
            contenedorRanking = GameObject.Find("Content")?.transform;

        if (rankingEntryPrefab == null)
            rankingEntryPrefab = Resources.Load<GameObject>("Prefabs/Panels/Panel_PlayerEntry");

        if (avatarManager == null)
            avatarManager = Object.FindFirstObjectByType<AvatarManager>();

        if (medallasTop3 == null || medallasTop3.Count < 3)
        {
            medallasTop3 = new List<Sprite>
            {
                Resources.Load<Sprite>("Sprites/oro_0"),
                Resources.Load<Sprite>("Sprites/plata_0"),
                Resources.Load<Sprite>("Sprites/bronce_0")
            };
        }
    }
}
