using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UseWeapon : MonoBehaviour {

    /// <summary>
    /// Handles all actions a player may do using a weapon
    /// </summary>


    #region Inspector Var
    public string primary;
    public string secondary;
    public int currentMagazine;
    public int reserveAmmo;
    #endregion

    [HideInInspector]
    public Weapon primaryWeapon;
    [HideInInspector]
    public Weapon secondaryWeapon;


    private MouseLook mouseLook;
    private Camera playerCamera;
    private PoolManager poolManager;
    private Points points;
    private MovePlayer movePlayer;
    private WeaponManager weaponManager;
    [SerializeField] private Transform melee;


    [HideInInspector] public float firingTime;
    [HideInInspector] public bool canFire;

    [HideInInspector] public bool isReloading;
    [HideInInspector] public float reloadTimer;
    [HideInInspector] public float chamberTimer;

    [HideInInspector] public bool isSwitching;
    [HideInInspector] public float switchTimer;

    [HideInInspector] public bool isInteracting;
    [HideInInspector] public float interactTimer;
    public float interactTime;

    [HideInInspector] public float instaKillTimer;
    public float instaKillTime;

    [Header("~~")]
    public bool zombieInRange;
    public float meleeDamage;
    public float buyRange;
    private GiveGun buyZone;
    public LayerMask ignoreMask;
    public LayerMask whatIsBuyZone;
    public LayerMask zombieLayer;

    [HideInInspector] public bool isZoomed;

    Coroutine PurchaseWeapon;
    Coroutine FireWeapon;

    //pierce
    //range
    #region MonoBehaviour Awake(), Start()

    private void Awake() {
        //local components
        movePlayer = gameObject.GetComponent<MovePlayer>();
        mouseLook = gameObject.GetComponent<MouseLook>();
        playerCamera = mouseLook.playerCamera;
        points = gameObject.GetComponent<Points>();
        weaponManager = playerCamera.GetComponentInParent<WeaponManager>();
        //global components
        poolManager = PoolManager.instance;
        //events
        PowerupScript.MaxAmmo += MaxAmmo;
        PowerupScript.InstaKill += InstaKill;
    }

    private void Start() {
        ignoreMask = ~ignoreMask;
        primaryWeapon = new Pistol();
        secondaryWeapon = new None();
        weaponManager.SetActiveWeapon(primaryWeapon.WeaponName);
        canFire = true;
    }
    #endregion
    #region MonoBehaviour Update()
    private void Update() {
        ProcessReload();
        Fire();
        SwitchWeapon();
        ShowInspector();
        DoPurchase();
        DoMelee();

        if (isInteracting) {
            mouseLook.LockMouseInput(true);
            movePlayer.LockMovement(true);
        }
        else {
            mouseLook.LockMouseInput(false);
            movePlayer.LockMovement(false);
        }

        if (Input.GetMouseButton(1) && !isReloading) {
            isZoomed = true;
            mouseLook.ZoomWeapon(primaryWeapon.ZoomValue);
            movePlayer.ZoomedIn(primaryWeapon.ZoomMoveSpeed);
            weaponManager.currentAnimator.MoveToFace();
        }
        else {
            isZoomed = false;
            mouseLook.UnZoom();
            movePlayer.UnZoom();
            weaponManager.currentAnimator.MoveToHand();
        }

        if (instaKillTimer > 0) {
            instaKillTimer -= Time.deltaTime;
        }
    }
    #endregion
    #region Inspector
    /*
     * This class has its own primary and secondary fields to store the currently used weapons names and ammo amounts (for ease of access)
     * 
     * DO NOT GET THESE CONFUSED WITH THE PRIMARY/SECONDARY WEAPON CLASSES VARIABLES OF SIMILAR NAMES
     */
    private void ShowInspector() {
        primary = primaryWeapon.WeaponName;
        secondary = secondaryWeapon.WeaponName;
        currentMagazine = primaryWeapon.CurrentMag;
        reserveAmmo = primaryWeapon.ReserveAmmo;
    }
    #endregion
    #region Reload
    /*
     * If player has ammo, is free from actions, doesn't currently have a full magazine, and has reserve ammo start a Reload
     * 
     * IF WEAPON IS MAGAZINE FED
     * Get the required amount of ammo needed in the magazine to be at max
     * Set the magazine to the max ammount
     * Subtract the required amount from the reserve ammo
     * If the required amount of ammo is not owned
     * Add all reserve ammo to the magazine, set reserve ammo to 0
     * 
     * IF WEAPON IS NOT MAGAZINE FED
     * Call the open chamber time once starting a reload
     * Wait reloadSpeed duration
     * Add 1 bullet to gun
     * Subtract 1 bullet from reserves
     * repeat until full
     */

    private void ProcessReload() {
        if (Input.GetKeyDown(KeyCode.R) && CanOperate() && primaryWeapon.CurrentMag != primaryWeapon.MaxMag && primaryWeapon.ReserveAmmo > 0) {
            StartCoroutine(Reload());
        }
        else if (primaryWeapon.CurrentMag == 0 && CanOperate() && primaryWeapon.ReserveAmmo > 0) {
            StartCoroutine(Reload());
        }
    }

    IEnumerator Reload() {
        isReloading = true;
        if (primaryWeapon.ClipFed) {
            while (isReloading) {
                weaponManager.currentAnimator.Reload(primaryWeapon.ReloadSpeed);
                reloadTimer = primaryWeapon.ReloadSpeed;
                yield return new WaitForSeconds(primaryWeapon.ReloadSpeed);
                int ammoRequired = primaryWeapon.MaxMag - primaryWeapon.CurrentMag;
                if (primaryWeapon.ReserveAmmo > ammoRequired) {
                    primaryWeapon.CurrentMag = primaryWeapon.MaxMag;
                    primaryWeapon.ReserveAmmo -= ammoRequired;
                    isReloading = false;
                }
                else {
                    primaryWeapon.CurrentMag += primaryWeapon.ReserveAmmo;
                    primaryWeapon.ReserveAmmo = 0;
                    isReloading = false;
                }
            }
        }
        else if (!primaryWeapon.ClipFed) {
            chamberTimer = primaryWeapon.ChamberTime;
            yield return new WaitForSeconds(primaryWeapon.ChamberTime);
            while (isReloading && primaryWeapon.CurrentMag < primaryWeapon.MaxMag && primaryWeapon.ReserveAmmo > 0) {
                weaponManager.currentAnimator.Reload(primaryWeapon.ReloadSpeed);
                reloadTimer = primaryWeapon.ReloadSpeed;
                yield return new WaitForSeconds(primaryWeapon.ReloadSpeed);
                primaryWeapon.CurrentMag++;
                primaryWeapon.ReserveAmmo--;
                yield return null;
            }
            isReloading = false;
        }
        yield break;
    }
    #endregion
    #region Switch
    /*
     * If player is free from any actions and presses Q (and they possess a second weapon)...
     * Store their primary weapon in a temporary obj
     * Make their secondary weapon their primary
     * Make their primary weapon their secondary
     */

    private void SwitchWeapon() {
        if (Input.GetKeyDown(KeyCode.Q) && CanOperate() && !(secondaryWeapon is None)) {
            StartCoroutine(Switch());
        }
    }

    public IEnumerator Switch() {
        isSwitching = true;
        switchTimer = secondaryWeapon.DrawTime;
        StartCoroutine(weaponManager.SwitchWeapons(secondaryWeapon.WeaponName, secondaryWeapon.DrawTime));
        yield return new WaitForSeconds(secondaryWeapon.DrawTime);
        Weapon temp = secondaryWeapon;
        secondaryWeapon = primaryWeapon;
        primaryWeapon = temp;
        isSwitching = false;
        yield break;
    }
    #endregion
    #region Firing
    /* 
     * Player will shoot a raycast with dist specified by the current weapon (primary weapon) and returns the first hit collider, 
     * If the collider is layered a zombie or head, call the damage method on the hit zombie...
     * Otherwise, place a bullet hole decal on the obj specified by the primaryWeapon's bullet hole size.
     */
    private void Fire() {
        //if (firingTime > 0) firingTime -= Time.deltaTime;
        if (Input.GetMouseButton(0) && CanOperate() && primaryWeapon.CurrentMag > 0 && canFire) { // NOT FULL AUTO
            FireWeapon = StartCoroutine(FireAllProjectiles());
        }
    }

    IEnumerator FireAllProjectiles() {
        canFire = false;
        primaryWeapon.CurrentMag--;
        weaponManager.currentAnimator.Fire(primaryWeapon.FiringRate);
        mouseLook.Recoil(primaryWeapon.Recoil);
        ZombieHealth hitZombie;
        if (primaryWeapon.Pierce) {
            for (int shots = primaryWeapon.Projectiles; shots > 0; shots--) {
                //  if(Physics.RaycastAll

            }
        }
        for (int shots = primaryWeapon.Projectiles; shots > 0; shots--) {

            Vector3 bulletDir = playerCamera.gameObject.transform.forward + playerCamera.transform.TransformDirection
            (new Vector3(UnityEngine.Random.Range(-primaryWeapon.BulletSpreadRadius, primaryWeapon.BulletSpreadRadius),
            UnityEngine.Random.Range(-primaryWeapon.BulletSpreadRadius, primaryWeapon.BulletSpreadRadius)));
            if (primaryWeapon.Pierce) {
                //if(Physics.RaycastAll(playerCamera.gameObject.transform.position, bulletDir, out RaycastHit[] hits, primaryWeapon.MaxRange, playerMask)) {

                //}
            }

            if (Physics.Raycast(playerCamera.gameObject.transform.position, bulletDir, out RaycastHit hit, primaryWeapon.MaxRange, ignoreMask)) {

                if (hit.transform.gameObject.CompareTag("Zombie Head")) {
                    hitZombie = hit.collider.gameObject.GetComponentInParent<ZombieHealth>();
                    hitZombie.TakeDamage(Damage(true, hit.distance), CanInstaKill());
                    points.AddPoints(primaryWeapon.PointValue, primaryWeapon.PointMultiplier);
                }
                else if (hit.transform.gameObject.CompareTag("Zombie Body")) {
                    hitZombie = hit.collider.gameObject.GetComponentInParent<ZombieHealth>();
                    hitZombie.TakeDamage(Damage(false, hit.distance), CanInstaKill());
                    points.AddPoints(primaryWeapon.PointValue);
                }
                else {
                    Quaternion rot = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                    poolManager.SpawnFromPool(primaryWeapon.BulletHoleSize, hit.point, rot);
                }
            }
            yield return null;
        }
        yield return new WaitForSeconds(primaryWeapon.FiringRate);
        StartCoroutine(ResetTrigger());
        yield break;
    }

    IEnumerator ResetTrigger() {
        if (!primaryWeapon.Automatic) {
            while (Input.GetMouseButton(0)) {
                canFire = false;
                yield return null;
            }
        }
        canFire = true;
        yield break;
    }


    #endregion
    #region Return Methods
    public bool CanPurchase() { //if player is looking at a buy zone, return true
        if (Physics.Raycast(playerCamera.gameObject.transform.position, playerCamera.gameObject.transform.forward, buyRange, whatIsBuyZone)) {
            return true;
        }
        else { return false; }
    }

    public bool CanOperate() { //if player is not currently in an action, return true
        if (isReloading || isInteracting || isSwitching) {
            return false;
        }
        else { return true; }
    }

    private int PurchaseCost() {
        if (!secondaryWeapon.WeaponName.Equals(buyZone.weapon) && !primaryWeapon.WeaponName.Equals(buyZone.weapon)) {
            // if weapon is not currently owned
            return buyZone.cost;
        }
        else {
            // if weapon is obtained, purchase ammo instead
            return buyZone.ammoCost;
        }
    }

    private float Damage(bool headshot, float distance) {
        float damage = primaryWeapon.BulletDamage * Mathf.Pow(primaryWeapon.DamageFalloff, distance / -100);
        Debug.Log(damage);
        if (headshot) { return damage * primaryWeapon.HeadshotMultiplier; }
        else { return damage; }

    }

    private bool CanInstaKill() {
        if (instaKillTimer > 0) {
            return true;
        }
        return false;
    }

    #endregion
    #region Weapon Buying
    /*
     * If player is looking at a buy zone and free from any actions, start a purchase
     * Get the current buy zone they are looking at
     * If player is still looking at buy zone (their sensitivity will be locked) and holding E for seconds specified by interactTime...
     * The player will either get a secondary, replace their current gun, or buy ammo
     */

    private void DoPurchase() {
        if (CanPurchase() && CanOperate() && Input.GetKeyDown(KeyCode.E)) {
            isInteracting = true;
            Physics.Raycast(playerCamera.gameObject.transform.position, playerCamera.gameObject.transform.forward, out RaycastHit hit, buyRange, whatIsBuyZone);
            buyZone = hit.transform.gameObject.GetComponent<GiveGun>();
            //StartCoroutine(Purchasing());
            if (points.CanAfford(PurchaseCost())) {
                PurchaseWeapon = StartCoroutine(Purchasing());
            }
        }

        if (Input.GetKeyUp(KeyCode.E)) {
            if (PurchaseWeapon != null) {
                StopCoroutine(PurchaseWeapon);
            }
            isInteracting = false;
        }
    }

    IEnumerator Purchasing() {
        while (CanPurchase() && isInteracting) {
            interactTimer = interactTime;
            yield return new WaitForSeconds(interactTime);
            DecidePurchase();
            isInteracting = false;
            yield break;
        }
        isInteracting = false;
        yield break;
    }

    private void DecidePurchase() {
        if (!secondaryWeapon.WeaponName.Equals(buyZone.weapon) && !primaryWeapon.WeaponName.Equals(buyZone.weapon)) { // if weapon is not currently owned
            points.RemovePoints(buyZone.cost);
            if (secondaryWeapon is None) { //if no current secondary, make weapon secondary then switch weapons
                Debug.Log("Secondary Purchased");
                secondaryWeapon = (Weapon)Activator.CreateInstance(buyZone.weaponType);
                Weapon temp = secondaryWeapon;
                secondaryWeapon = primaryWeapon;
                primaryWeapon = temp;
                weaponManager.SetActiveWeapon(buyZone.weapon);
            }
            else { //if player has two weapons, make their current weapon the purchassed weapon
                Debug.Log("Primary Purchased");
                primaryWeapon = (Weapon)Activator.CreateInstance(buyZone.weaponType);
                weaponManager.SetActiveWeapon(buyZone.weapon);
            }
        }
        else { //if weapon is obtained, purchase ammo instead
            if (primaryWeapon.WeaponName.Equals(buyZone.weapon)) {
                points.RemovePoints(buyZone.ammoCost);
                Debug.Log("Ammo Purchased");
                primaryWeapon.ReserveAmmo = primaryWeapon.MaxReserveAmmo;
            }
            else if (secondaryWeapon.WeaponName.Equals(buyZone.weapon)) {
                points.RemovePoints(buyZone.ammoCost);
                Debug.Log("Ammo Purchased");
                secondaryWeapon.ReserveAmmo = secondaryWeapon.MaxReserveAmmo;
            }
        }
    }

    #endregion
    #region Melee

    private void DoMelee() {
        if (Physics.SphereCast(melee.position, 0.5f, melee.forward, out RaycastHit hit, 0.5f, zombieLayer)) {
            if (Input.GetKeyDown(KeyCode.V)) {
                Debug.Log("melee hit");
                ZombieHealth zombie = hit.transform.gameObject.GetComponent<ZombieHealth>();
                zombie.TakeDamage(meleeDamage, CanInstaKill());
            }
        }
        if(Input.GetKeyDown(KeyCode.V)) {
            Debug.Log("melee");
        }
    }
    private void OnDrawGizmos() {
        Gizmos.DrawWireSphere(melee.position, 0.5f);
    }

    #endregion
    #region Debug
    public void SpawnWeapon(string weaponType) {
        Type weapon = Type.GetType(weaponType);
        primaryWeapon = (Weapon)Activator.CreateInstance(weapon);
        weaponManager.SetActiveWeapon(weaponType);
    }
    #endregion
    #region Powerups
    private void MaxAmmo() {
        primaryWeapon.ReserveAmmo = primaryWeapon.MaxReserveAmmo;
        secondaryWeapon.ReserveAmmo = secondaryWeapon.MaxReserveAmmo;
    }

    private void InstaKill() {
        instaKillTimer = instaKillTime;
    }
    #endregion
}
