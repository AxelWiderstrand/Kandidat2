using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Threading;
using System.Threading.Tasks;
using System;

public class CITENetworkManager : NetworkManager {
    private Dictionary<NetworkConnection,int> connectedIDs;
    public int bootstrapSceneBuildIndex;
    public int errorSceneBuildIndex;

    public override void Awake(){
        base.Awake();
	    initialize();
    }

    public void initialize(){
        Debug.Log("Loading bootstrap");
        Application.targetFrameRate = 60;
        connectedIDs = new Dictionary<NetworkConnection, int>();
        //SceneManager.LoadScene()
        SceneManager.LoadScene(bootstrapSceneBuildIndex, LoadSceneMode.Single);
    }

    public override void OnDestroy(){
        base.OnDestroy();
        StopHost(); // De-allocate network sockets etc immediately on destruction
    }

    /**
     * Perform an action when someone joins the server run by this network manager
     */
    public override void OnServerConnect(NetworkConnectionToClient conn){
        // Find the first free player ID not already in use
        int newPlayerID = 0;
        while (connectedIDs.ContainsValue(newPlayerID)){
            newPlayerID++;
        }
        connectedIDs.Add(conn, newPlayerID);

        Debug.Log("Added connection for player with ID " + newPlayerID);
        base.OnServerConnect(conn);
    }

    /**
     * Remove people from the connection list when they leave the server run by this manager
     */
    public override void OnServerDisconnect(NetworkConnectionToClient conn){
        Debug.Log("Player " + connectedIDs[conn] + " disconnected");
        connectedIDs.Remove(conn);

        // For now we intentionally crash everything since disconnection handling is not part of the API yet
	    Debug.Log("Someone disconnected, rebooting everything...");
        error();
    }

    /** 
     * Lookup a player's ID using the connection
     */
    public int GetPlayerID(NetworkConnection conn){
        return connectedIDs[conn];
    }

    /**
     * Counts the total number of players currently connected
     */
    public int GetConnectionCount(){
        return connectedIDs.Count;
    }

    private int PAUSE_DIE_TIME = 5;
    private float whenPaused = -1;
    private CancellationTokenSource cancelSource = null;

    /**
     * Handle the application being paused/losing focus
     */
    public void OnApplicationPause(bool paused){
        if(cancelSource != null) {
            cancelSource.Cancel();
            cancelSource = null;
        }

        if(paused) {
            //We are now paused, remember when it happened
            whenPaused = Time.realtimeSinceStartup;

            Debug.Log("Paused...");

            cancelSource = new CancellationTokenSource();

            Task.Run(async () => {
                await Task.Delay(TimeSpan.FromSeconds(PAUSE_DIE_TIME), cancelSource.Token);
                //TODO Kill new transport?

                //LiteNetLib4MirrorTransport t = (LiteNetLib4MirrorTransport)transport;
                //t.Shutdown();
                Debug.Log("Transport killed !!!NOT!!!...");
            });

        } else {
            float pauseTime = Time.realtimeSinceStartup - whenPaused;

            //No longer paused, check if more time passed than we allow
            if (whenPaused > 0 && pauseTime > PAUSE_DIE_TIME) {
                Debug.Log("Application was paused more than " + PAUSE_DIE_TIME + "sec, rebooting everything... ("+(Time.realtimeSinceStartup - whenPaused) +"sec)");
                error();
            } else {
                Debug.Log("Came pack from small pause: " + pauseTime + "sec");
            }

            whenPaused = -1;
        }
    }

    /**
     * Handle being disconnected from a running game server
     */
    public override void OnClientDisconnect(){
        // For now we intentionally crash the application since being kicked is not yet part of the API
    	Debug.Log("This client was disconnected, rebooting everything...");
    	error();
    }

    /**
     * An error occoured, handle it
     */
    public void error(){
	    Debug.Log("------------------------->>> REBOOT <<<-----------------------------------");
        SceneManager.LoadScene(errorSceneBuildIndex, LoadSceneMode.Single);
    }
}
