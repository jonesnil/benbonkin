﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Player : MonoBehaviour
{
    [SerializeField] float _movementSpeed;
    [SerializeField] RunTimeData _data;
    [SerializeField] GameObject bulletPrefab;
    Animator animator;
    float health;
    [SerializeField] float startingHealth;
    Color spriteColor;
    SpriteRenderer sprite;
    Rigidbody2D body;
    Collider2D collider;
    bool knockedBack;
    bool cantMove;
    Vector3 knockDirection;
    int knockBackFrame;
    [SerializeField] int knockBackFrames;
    [SerializeField] int noMoveFrames;
    [SerializeField] int iFrames;
    [SerializeField] float knockBackSpeed;
    Transform staff;
    Animator staffAnimator;
    SpriteRenderer staffSprite;
    bool runningAnimation;
    AudioSource hitNoise;
    [SerializeField] GameObject baddiePrefab;

    // Start is called before the first frame update
    void Start()
    {
        animator = this.GetComponent<Animator>();
        health = startingHealth;

        GameEvents.PlayerHit += OnPlayerHit;
        GameEvents.GameOver += OnGameOver;
        GameEvents.BeatLevel += OnBeatLevel;

        _data.startingHealth = startingHealth;
        _data.health = health;
        _data.iFrames = this.iFrames;
        sprite = this.GetComponent<SpriteRenderer>();
        body = this.GetComponent<Rigidbody2D>();
        collider = this.GetComponent<Collider2D>();
        knockedBack = false;
        cantMove = false;
        spriteColor = sprite.color;
        staff = this.transform.GetChild(0);
        staffAnimator = staff.GetComponent<Animator>();
        staffSprite = staff.GetComponent<SpriteRenderer>();
        runningAnimation = false;
        hitNoise = this.GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();

        Shoot();

        if(knockedBack)
            KnockBack();

        StaffAim();

    }

    void Move() 
    {
        Vector3 HorizontalMovement = new Vector3(Input.GetAxis("Horizontal"), 0, 0);
        Vector3 VerticalMovement = new Vector3(0, Input.GetAxis("Vertical"), 0);
        Vector3 move = (HorizontalMovement + VerticalMovement) * Time.deltaTime * _movementSpeed;

        Collider2D collided = GetCollision(move);

        Vector3 sideMove = new Vector3(move.x, 0f, move.z);
        Collider2D collidedSide = GetCollision(sideMove);

        Vector3 vertMove = new Vector3(0f, move.y, move.z);
        Collider2D collidedVert = GetCollision(vertMove);

        if (collided != null && collided.gameObject.tag == "Exit") 
        {
            GameEvents.InvokeBeatLevel();
        }

        if (collided != null && collided.gameObject.tag == "Trap")
        {
            Destroy(collided.gameObject);
            collided = null;
            Vector3 trapPos = new Vector3(this.transform.position.x - 7f, this.transform.position.y, this.transform.position.z);
            Instantiate(baddiePrefab, trapPos, Quaternion.identity);
        }

        if (!cantMove)
        {
            if (collided == null)
                this.transform.position += move;

            else if (collidedSide == null)
                this.transform.position += sideMove;

            else if (collidedVert == null)
                this.transform.position += vertMove;

            else if (knockedBack && !(collided.tag == "Wall"))
                this.transform.position += move;
        }

        if (Input.GetAxis("Horizontal") < 0 && !sprite.flipX) 
        {
            sprite.flipX = true;
        }

        if (Input.GetAxis("Horizontal") > 0 && sprite.flipX)
        {
            sprite.flipX = false;
        }

        animator.SetFloat("moveSpeed", move.magnitude);

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Player_MoveRight") && !runningAnimation) 
        {
            runningAnimation = true;
            staff.localPosition = new Vector3(staff.localPosition.x, staff.localPosition.y + .01f, staff.localPosition.z);
        }

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Player_IdleRight") && runningAnimation)
        {
            runningAnimation = false;
            staff.localPosition = new Vector3(staff.localPosition.x, staff.localPosition.y - .01f, staff.localPosition.z);
        }

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Player_HitRight") && runningAnimation)
        {
            runningAnimation = false;
            staff.localPosition = new Vector3(staff.localPosition.x, staff.localPosition.y - .01f, staff.localPosition.z);
        }

        _data.playerPos = this.transform.position;

    }

    void Shoot() 
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!staffAnimator.GetBool("shot"))
            {
                Vector3 mouseDummy = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 mouseWorldPos = new Vector3(mouseDummy.x, mouseDummy.y, 0);
                Vector3 directionToMouse = (mouseWorldPos - this.transform.position);

                Bullet newBullet = Instantiate(bulletPrefab, this.transform.position, Quaternion.identity).GetComponent<Bullet>();
                newBullet.direction = directionToMouse * 10000;

                staffAnimator.SetBool("shot", true);
            }
        }
    }

    void StaffAim() 
    {
        if (Camera.main.ScreenToWorldPoint(Input.mousePosition).x > this.transform.position.x && staffSprite.flipX) 
        {
            staff.localPosition = new Vector3(-staff.localPosition.x, staff.localPosition.y, staff.localPosition.z);
            staffSprite.flipX = false;
        }

        if (Camera.main.ScreenToWorldPoint(Input.mousePosition).x < this.transform.position.x && !staffSprite.flipX)
        {
            staff.localPosition = new Vector3(-staff.localPosition.x, staff.localPosition.y, staff.localPosition.z);
            staffSprite.flipX = true;
        }
    }

    void OnPlayerHit(object sender, BaddieEventArgs args) 
    {
        if (!knockedBack)
        {
            Baddie baddie = args.baddiePayload;
            this.health -= baddie.damage;
            _data.health = health;
            animator.SetBool("hit", true);
            knockedBack = true;
            cantMove = true;
            knockBackFrame = 0;
            knockDirection = (this.transform.position - baddie.gameObject.transform.position).normalized * knockBackSpeed * Time.deltaTime;

            hitNoise.Play();
            baddie.Die();
        }
    }



    Collider2D GetCollision(Vector3 direction) 
    {
        RaycastHit2D[] results = new RaycastHit2D[4];

        Vector3 topRight = new Vector3(transform.position.x + .25f, transform.position.y, transform.position.z);
        Vector3 topLeft = new Vector3(transform.position.x - .25f, transform.position.y, transform.position.z);
        Vector3 bottomRight = new Vector3(transform.position.x + .25f, transform.position.y - .5f, transform.position.z);
        Vector3 bottomLeft = new Vector3(transform.position.x - .25f, transform.position.y - .5f, transform.position.z);
        Vector3[] colliderCheckPoints = { topRight, topLeft, bottomRight, bottomLeft };

        Collider2D collided = null;

        int index = 0;

        while (index < 4)
        {
            results[index] = Physics2D.Raycast(colliderCheckPoints[index], direction, direction.magnitude, ~(1 << 8));
            if (results[index].collider != null)
            {
                collided = results[index].collider;
            }
            index += 1;
        }

        return collided;
    }
    void KnockBack() 
    {
        Collider2D collided = GetCollision(knockDirection);

        Vector3 sideMove = new Vector3(knockDirection.x, 0f, knockDirection.z);
        Collider2D collidedSide = GetCollision(sideMove);

        Vector3 vertMove = new Vector3(0f, knockDirection.y, knockDirection.z);
        Collider2D collidedVert = GetCollision(vertMove);

        if (knockBackFrame <= knockBackFrames)
        {
            if (collided == null)
                this.transform.position += knockDirection;
           
            else if (collidedSide == null)
                this.transform.position += sideMove;

            else if (collidedVert == null)
                this.transform.position += vertMove;
        }

        knockBackFrame += 1;

        if (knockBackFrame >= noMoveFrames && cantMove) 
        {
            cantMove = false;
        }

        if (knockBackFrame >= iFrames) 
        {
            knockedBack = false;
            knockBackFrame = 0;
        }
    }

    void OnBeatLevel(object sender, EventArgs args)
    {
        GameEvents.PlayerHit -= OnPlayerHit;
        GameEvents.GameOver -= OnGameOver;
        GameEvents.BeatLevel -= OnBeatLevel;
    }

    void OnGameOver(object sender, EventArgs args) 
    {
        GameEvents.PlayerHit -= OnPlayerHit;
        GameEvents.GameOver -= OnGameOver;
        GameEvents.BeatLevel -= OnBeatLevel;
    }

}
