using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ArenaPlayerUI : NetworkBehaviour
{

    public Image HealthBar;
    public Image ToxicVignette;
    public Image DoubleDamage;
    public Image TakenDamageVignette;

    public TMP_Text AmmoText;
    public TMP_Text GrenadeText;

    private Health _health;

    private PlayerController _playerController;

    private PlayerWeaponController _playerWeaponController;

    private GrenadeWeaponManager _grenadeWeaponManager;

    private float _takenDamageTimer = 0f;

    #region Start
    void Start()
    {
        _health = GetComponentInParent<Health>();

        _playerController = GetComponentInParent<PlayerController>();

        _playerWeaponController = GetComponentInParent<PlayerWeaponController>();

        _grenadeWeaponManager = GetComponentInParent<GrenadeWeaponManager>();
    }
    #endregion

    #region OnNetworkSpawn
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            gameObject.SetActive(false);
        }
    }
    #endregion

    #region Update
    void Update()
    {
        if (!IsOwner) return;

        // Updates the UI

        HealthBar.fillAmount = _health.GetHealthNormalized();



        Color color = ToxicVignette.color;

        color.a = Mathf.Lerp(0f, 1f, _playerController.CSGasEffectTimer.Value / _playerController.CSGasEffectDuration);

        ToxicVignette.color = color;



        DoubleDamage.gameObject.SetActive(_playerWeaponController.DoubleDamageTimer.Value > 0);



        GrenadeText.text = $"Gas\n{_grenadeWeaponManager.GrenadeCount.Value}";



        if (_takenDamageTimer > 0f) _takenDamageTimer -= Time.deltaTime;


        Color damageColor = TakenDamageVignette.color;

        damageColor.a = _takenDamageTimer;

        TakenDamageVignette.color = damageColor;
    }
    #endregion

    #region 
    public void TakeDamage()
    {
        _takenDamageTimer = 1f;
    }
    #endregion
}
