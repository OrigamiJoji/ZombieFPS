using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAnimation : MonoBehaviour {
    public Animator ani;
    public Transform face;
    public Transform hand;
    public ParticleSystem flash;
    public float reloadTime;
    public float fireTime;

    private void Awake() {
        ani = gameObject.GetComponent<Animator>();
    }
    private void Start() {
        AnimationClip[] clips = ani.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips) {
            if(clip.name == gameObject.name + "Reloading") {
                reloadTime = clip.length;
            }
            else if(clip.name == gameObject.name + "Fire") {
                fireTime = clip.length;
            }
        }
    }

    public void Fire() {
        ani.SetTrigger("Fire");
        flash.Play();
    }

    public void Reload(float speed) {
        //it took me an ungodly amount of time to get this one equation 
        float mult = (reloadTime/speed);
        //what no math class for 2 years does to an mf
        ani.SetFloat("reloadMultiplier", mult);
        ani.SetTrigger("Reload");
    }

    public void MoveToFace() {
        transform.position = face.position;
    }

    public void MoveToHand() {
        transform.position = hand.position;
    }
}
