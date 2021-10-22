using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Main Gun behavior script.
/// </summary>
public class Gun : MonoBehaviour
{
    /// <summary>
    /// Prefab used for the bullets.
    /// </summary>
    [ Header("Global") ]
    public GameObject bulletPrefab;
    
    /// <summary>
    /// Fire rate in bullets per minute.
    /// </summary>
    [ Header("Gameplay") ]
    public float fireRate = 60.0f;
    
    /// <summary>
    /// Can this gun fire?
    /// </summary>
    public bool fireEnabled = true;

    /// <summary>
    /// Enable to switch the weapon to "shotgun mode".
    /// </summary>
    public bool shotgun = false;
    
    /// <summary>
    /// Number of bullets in one shotgun spread.
    /// </summary>
    public int shotgunBullets = 6;
    
    /// <summary>
    /// Shotgun spread angle in degrees.
    /// </summary>
    public float shotgunSpread = 30.0f;
    
    /// <summary>
    /// Offset at which the bullet should be spawned.
    /// </summary>
    public float spawnOffset = 0.4f;

    public float shotgunFireRate = 100;
    
    /// <summary>
    /// Is the gun currently firing bullets?
    /// </summary>
    private bool mFiring = false;
    
    /// <summary>
    /// Time accumulator representing time before next shot in seconds.
    /// </summary>
    private float mCoolDown = 0.0f;

    /// <summary>
    /// Are we using the ECS to spawn the bullets?
    /// </summary>
    private bool mUseECS = false;

    /// <summary>
    /// Entity manager used for the spawning.
    /// </summary>
    private EntityManager mEntityManager;

    /// <summary>
    /// Storage for our blobs.
    /// </summary>
    private BlobAssetStore mBlobAssetStore;
    
    /// <summary>
    /// Entity prefab used as a base for each entity.
    /// </summary>
    private Entity mBulletEntityPrefab;

    /// <summary>
    /// Called before the first frame update.
    /// </summary>
    void Start()
    {
        mUseECS = Settings.Instance.useECS;
        if (mUseECS)
        { // Use the complete conversion workflow.
            // Get the entity manager for the main world.
            mEntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // Convert the Bullet GameObject Prefab to Entity.
            mBlobAssetStore = new BlobAssetStore();
            GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(
                World.DefaultGameObjectInjectionWorld, mBlobAssetStore
            );
            mBulletEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                bulletPrefab, settings
            );
#if UNITY_EDITOR
            if (Application.isEditor)
            { mEntityManager.SetName(mBulletEntityPrefab, "Bullet"); }
#endif // UNITY_EDITOR
        }
    }

    /// <summary>
    /// Cleanup when this script is destroyed.
    /// </summary>
    private void OnDestroy()
    { mBlobAssetStore?.Dispose(); }

    /// <summary>
    /// Update called at fixed time delta.
    /// </summary>
    void FixedUpdate()
    {
        // Cool down the weapon by the elapsed time.
        mCoolDown -= Time.deltaTime;
        
        if (mFiring && fireEnabled)
        {
            // Number of seconds we should wait between bullet spawns.
            float secondsPerBullet;
            if (!shotgun)
                secondsPerBullet = 1.0f / (fireRate / 60.0f);
            else
                secondsPerBullet = 1 / (shotgunFireRate / 60f);

            while (mCoolDown <= 0.0f)
            { // Spawn corresponding number of bullets.
                ShootGun();
                
                // "Heat up" the gun.
                mCoolDown += secondsPerBullet;
            }
        }
    }

    /// <summary>
    /// Start firing the gun.
    /// </summary>
    public void StartFiring()
    {
        // Start firing.
        mFiring = true; 
        // Respect current cooldown, up to a single shot.
        mCoolDown = Math.Max(mCoolDown, 0.0f);
    }

    /// <summary>
    /// Stop firing the gun.
    /// </summary>
    public void StopFiring()
    {
        // Stop firing.
        mFiring = false;
    }

    /// <summary>
    /// Make the gun face given target.
    /// </summary>
    /// <param name="target">Target position to face.</param>
    public void ChangeTarget(Vector3 target)
    {
        // Get the direction vector.
        var playerToTarget = target - transform.position;
        
        // Allow only rotation in the XZ plane (play area).
        playerToTarget.y = 0.0f;
        playerToTarget.Normalize();
        
        // Rotate towards it.
        transform.rotation = Quaternion.LookRotation(playerToTarget);
    }

    /// <summary>
    /// Shoot the gun using provided transform as a director..
    /// </summary>
    public void ShootGun()
    {
        var tf = transform;

        if (!shotgun)
        {
            SpawnBullet(
            tf.position + tf.forward * spawnOffset,
                tf.rotation
            );
        }
        else
        {
            for (var i = 0; i < shotgunBullets; i++)
            {
                float maxDeviation = shotgunSpread * .5f;
                float rotationDeviation = Random.Range(-maxDeviation, maxDeviation);
                tf.Rotate(0, rotationDeviation, 0);
                SpawnBullet(
                    tf.position + tf.forward * spawnOffset,
                    tf.rotation);
            }
        }
    }

    /// <summary>
    /// Spawn a single bullet using provided transform as a director.
    /// </summary>
    public void SpawnBullet(Vector3 position, Quaternion rotation)
    {
        if (mUseECS)
        { // Using ECS -> Spawn new entity.
            var bullet = mEntityManager.Instantiate(mBulletEntityPrefab);
            mEntityManager.SetComponentData(bullet, new Translation{ Value = position });
            mEntityManager.SetComponentData(bullet, new Rotation{ Value = rotation });
        }
        else
        { // Using default -> Spawn new GameObject.
            // Instantiate the bullet and set it up.
            var bullet = Instantiate(bulletPrefab);
            bullet.transform.position = position;
            bullet.transform.rotation = rotation;
        }
    }
    
    /// <summary>
    /// Adjust the rate of fire.
    /// </summary>
    /// <param name="magnitude">By how much should the fire-rate change.</param>
    public void AdjustFireRate(float magnitude)
    {
        // Clamp the values to be at least 1.0f.
        fireRate = Math.Max(fireRate + magnitude * 10.0f, 1.0f);
    }
    
    /// <summary>
    /// Adjust the number of bullets per spread.
    /// </summary>
    /// <param name="magnitude">By how much should the spread size change.</param>
    public void AdjustSpreadSize(int magnitude)
    {
        // Clamp the values to be at least 1.0f.
        shotgunBullets = Math.Max(shotgunBullets + magnitude, 1);
    }
}
