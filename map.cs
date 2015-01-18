using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Map
{
	//The name of the map
	public string Name { get; private set; }

	//These vectors represent the maximum and minimum values of the x, y and z coordinates of the GameObjects that make up the map
	//In most cases, no one GameObject in the map will match all three x, y and z values, they're taken from all the GameObjects
	//But together, these vectors can be used to figure out, for instance, the dimensions of the map as a whole, or the mid-point of the map
	public Vector3 MaxValues;
	public Vector3 MinValues;

	//The empty GameObject that serves as a parent for all the other GameObject that make up the map
	GameObject g_oAnchor;

	//An index of all the rooms in the map, using the names of the rooms as a key
	Dictionary<string, Room> g_oRooms;

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	//Description: Constructor.  The constructor accepts a string describing the map.
	//             The string takes the following form: "Map1/First Room|n:Second Room|e:Fourth Room/Second Room|s:First Room|e:Third Room/Third Room|w:Second Room|s:Fourth Room/Fourth Room|n:Third Room|w:First Room"
	//             The name of the map the entries for each room are separated by the '/' character
	//             These subsections are further divided into sub-subsections of the name of the room and the information about the rooms that are attached the the named room.  They are divided by the '|' character
	//             These sub-subsections are even further divided into sub-sub-subsections consisting of a direction used for drawing and positioning the GameObjects created to represent the map
	//             another direction the corresponds to the actual true direction used in the Inform7 code, and the name of the room that lies in that direction.  These are divided by the ':' character
	//             So: draw direction:true direction:room name
	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	public Map(string szMapScript)
	{
		//Instantiate the room index
		g_oRooms = new Dictionary<string, Room> ();

		//The name of the map will be the first entry in the map string
		Name = szMapScript.Split ('/') [0];

		//Remove the map name and the first '/' from the start of the MapString by getting the substring
		string[] szRoomsArray = szMapScript.Substring(Name.Length + 1).Split ('/');

		//Create the map
		buildMap (szRoomsArray);
	}

	//Flags a room as having been visited
	public void SetRoomToVisited(string szRoomName)
	{
		//Set the "visited" boolean of the room to true
		g_oRooms [szRoomName].Visited = true;
	}

	public bool ContainsRoom(string szRoomName)
	{
		if (g_oRooms.ContainsKey(szRoomName))
		    return true;
		else
			return false;
	}

	//----------------------------------------------------------------------------------------------------------------------------------
	//Description: A breadth-first search of the map to find the shortest path from one room to another
	//             Returns a string array containing the true directions required to get from the starting room to the destination room
	//----------------------------------------------------------------------------------------------------------------------------------
	public string[] FindPath(string szStart, string szDestination)
	{
		//The room currently being checked
		Room oCurrentRoom;
		//The sequence of directions taken to get from the starting room to the current room
		string[] szDirections;
		//The sequence of directions taken to get from the starting room to a room adjoining the current room
		string[] szAdjoiningDirections;
		//Instantiate a queue of string arrays
		//Each string array contains a sequence of directions that lead from the starting room to one of the rooms in the map
		Queue<string[]> oDirectionsToCheck = new Queue<string[]> ();
		//Instantiate a list of the rooms that have already been checked
		List<string> oCheckedRooms = new List<string> ();
		
		//The first string array added to the queue is empty (ie: it points to the starting room)
		oDirectionsToCheck.Enqueue (new string[0]);
		
		//While there's still string arrays in the queue
		while (oDirectionsToCheck.Count > 0)
		{
			//Make the current room the starting room
			oCurrentRoom = g_oRooms[szStart];
			
			//Get an array of directions from the queue
			szDirections = oDirectionsToCheck.Dequeue();
			
			//This loop iterates through the directions in the string array, and each time it re-assigns the value of the current room to the value of the room that lies in that direction
			//Thus, by starting at the starting room, it moves from there to the room to be checked next
			foreach (string szDirection in szDirections)
				oCurrentRoom = oCurrentRoom.GetRoomForTraversing(szDirection);

			//If the name of the current room is the name of the destination
			if (oCurrentRoom.Name == szDestination)
				//return the string array containing the sequence of directions used to get to the destination room from the starting room
				return szDirections;
			//If the current room isn't the destination room
			else
			{
				//Add the current room to the list of rooms that have been checked
				oCheckedRooms.Add (oCurrentRoom.Name);
				
				//For each of the drections leading from the current room
				foreach (string szDirection in oCurrentRoom.GetTrueDirections())
				{
					//If the adjoining room in the chosen direction hasn't already been checked, and has been visited
					if (!oCheckedRooms.Contains(oCurrentRoom.GetRoomForTraversing(szDirection).Name) && oCurrentRoom.Visited)
					{
						//Create a new array that has one more index than the current directions
						szAdjoiningDirections = new string[szDirections.Length + 1];
						
						//Iterate through the current directions
						for (int iIndex = 0; iIndex < szDirections.Length; iIndex++)
							//Add the current directions to the new string array
							szAdjoiningDirections[iIndex] = szDirections[iIndex];
						
						//ANd then add the direction leading off from the current room to an adjoining room
						szAdjoiningDirections[szDirections.Length] = szDirection;
						
						//Add the new array to the queue
						oDirectionsToCheck.Enqueue(szAdjoiningDirections);
					}
				}
			}
		}
		
		//If the destination room can't be found, return an empty string array
		//Which contains no directions, and thus points to the starting room
		return new string[0];
	}

	//----------------------------------------------------------------------------------------------
	//Descripion: Calls the private function that generates the GameObjects that make up the map
	//            Sets the maximum and minimum vectors that define the dimensions of the map
	//            And repositions that parent GameObject of the map into the very centre of the map
	//----------------------------------------------------------------------------------------------
	public GameObject DrawMap(string szCurrentRoomName)
	{
		//The new anchor GameObject will become the parent of all the room and passage GameObjects after they are drawn
		GameObject oNewAnchor = new GameObject ();

		//The initial parent of the rooms and passages of the map
		g_oAnchor = new GameObject ();

		//Draw the rooms and passages of the map
		drawRooms (g_oRooms [szCurrentRoomName], 0, 0, 0, new List<string>());

		//Set the maximum and minium vectors for the map, which can be done once there are rooms and passages drawn
		SetMaxMinValues ();

		//Make the new anchor a child of the initial anchor
		oNewAnchor.transform.parent = g_oAnchor.transform;
		//Scale it so that it's the same size as the initial anchor
		oNewAnchor.transform.localScale = new Vector3 (1, 1, 1);
		//Position it in the centre of all the rooms and passages
		//(This is done by adding the maximum and minimum vectors and dividing the result by 2)
		oNewAnchor.transform.localPosition = (MinValues + MaxValues) / 2;
		//Make the new anchor a child of nothing
		oNewAnchor.transform.parent = null;

		//Iterate through all the child transforms of the initial anchor
		foreach (Transform oChild in g_oAnchor.GetComponentsInChildren<Transform>())
			//Make the new anchor their parent
			oChild.transform.parent = oNewAnchor.transform;

		//Destroy the initial anchor
		MonoBehaviour.Destroy (g_oAnchor);

		//Make the new anchor as the global anchor variable
		g_oAnchor = oNewAnchor;

		//Return the global anchor variable
		return g_oAnchor;
	}

	//--------------------------------------------------------------------------------------------------------------------------------------------
	//Description: Create the GameObjects representing the rooms in the map (as cubes) and the paths between the rooms (as connective rectangles)
	//             by recursively traversing the rooms in the map and the connections between them.
	//             Rooms that have not been flagged as visited will not be drawn.
	//--------------------------------------------------------------------------------------------------------------------------------------------
	void drawRooms (Room oCurrentRoom, float fX, float fY, float fZ, List<string> oCompleted)
	{
		//The next room to be drawn
		Room oNextRoom;
		//The connection between the current room and the next room
		GameObject oConnection;
		//The distance between directly adjacent rooms
		float fRoomInterval;
		//The distance between diagonally adjacent rooms
		float fHypotenuse;

		//The x, y and z offsets from the current room's position for the next room to be drawn
		float fXOffset;
		float fYOffset;
		float fZOffset;

		//A variable that contains the distance and direction information extracted from the distance and direction string via regular expressions
		Match oMatch;
		//The direction of the next room from the current room
		string szDirection;
		//The distance between the current room and the next room
		int iDistance;

		//Add the current room to the List of completed map components
		oCompleted.Add (oCurrentRoom.Name);

		//Create the current room GameObject
		GameObject oRoomCube = GameObject.CreatePrimitive (PrimitiveType.Cube);
		//Make the current room a child of the parent argument
		oRoomCube.transform.parent = g_oAnchor.transform;
		//Set the scale of the room to be 1 x 1 x 1
		oRoomCube.transform.localScale = new Vector3 (1, 1, 1);
		//Position the GameObject according to the X, Y, Z coordinate offset arguments
		oRoomCube.transform.localPosition = new Vector3 (fX, fY, fZ);
		//Give the GameObject the same name as the Room it represents
		oRoomCube.name = oCurrentRoom.Name;

		//For each distance and direction leading from the current room
		foreach (string szDistanceAndDirection in oCurrentRoom.GetDrawDirections())
		{
			//Reset the offsets to zero
			fXOffset = 0;
			fYOffset = 0;
			fZOffset = 0;

			//The regular expression finds zero or more numbers in the distance and direction string, and one or more letters, and groups them into two groups.
			//The input strings for this will always either just be a direction ("n", "ne", "s", "u", etc) or a distance and a direction ("2n", "3ne", "10s", etc)
			//So the value of group one should always be an integer number, or an empty string, and the value of group two should always be just a single direction.
			oMatch = Regex.Match(szDistanceAndDirection, @"(\d*)(\w+)");

			//If the value of the first group is not an empty string
			if (oMatch.Groups[1].Value != string.Empty)
				//Convert the value to an integer and assign it to the distance variable
				iDistance = Convert.ToInt32(oMatch.Groups[1].Value);
			else
				//Otherwise, the distance is assumed to be 1
				iDistance = 1;

			//The distance between adjacent rooms is the distance times two
			//Two is hard coded here, so here is where it should be changed if necessary.
			fRoomInterval = 2 * iDistance;

			//The distance between two diagonally adjacent rooms is the longer side of the triangle formed by connecting three rooms (in a corner shape) together
			//This will be a right-angled triangle, so a^2 + b^2 = c^2 applies, and it's also an isosceles triangle, so side a and side b are of equal length
			//Thus, the diagonal distance can be worked out by finding the square root of twice the square of the distance between directly adjacent rooms
			fHypotenuse = (float)Math.Sqrt(fRoomInterval * fRoomInterval * 2);

			//The direction is the value of the second group
			szDirection = oMatch.Groups[2].Value;

			//If a path joining the current room to the adjacent room (which will be named either current-room-name_adjacent-room-name or adjacent-room-name_current-room-name) doesn't already exist
			if (!oCompleted.Contains(oCurrentRoom.Name + "_" + oCurrentRoom.GetRoomForDrawing(szDistanceAndDirection).Name) && !oCompleted.Contains(oCurrentRoom.GetRoomForDrawing(szDistanceAndDirection).Name + "_" + oCurrentRoom.Name))
			{
				//Create the GameObject the represents the path between the rooms as a cube
				oConnection = GameObject.CreatePrimitive(PrimitiveType.Cube);
				//Make it a child of the parent argument
				oConnection.transform.parent = g_oAnchor.transform;
				//Scale it so that it has a width and depth of a quarter (of the room's dimensions), and a height equal to four fifths of the distance between rooms
				//(We use four fifths of the distance between rooms so that there's a visible gap in the map if the room has not been visited.)
				oConnection.transform.localScale = new Vector3 (0.25f, fRoomInterval * 0.8f, 0.25f);
				//Name the path "current-room-name_adjacent-room-name"
				oConnection.name = oCurrentRoom.Name + "_" + oCurrentRoom.GetRoomForDrawing(szDistanceAndDirection).Name;
				//Add the path to the list of completed map components
				oCompleted.Add(oConnection.name);

				//If the draw direction is "n" for north
				if (szDirection == "n")
				{
					//The adjacent room will be directly above the current room on the y-axis, so add the distance between rooms to the y offset
					fYOffset = fRoomInterval;
					//The default position of the paths is north-south, so the path doesn't need to be rotated
					//The position of the path is the mid-point between the current room and the adjacent room
					//So we add half the distance between rooms to the current room's y-axis position
					oConnection.transform.localPosition = oRoomCube.transform.localPosition + (new Vector3(0, fRoomInterval / 2, 0));
					
				}
				//If the draw direction is "s" for south
				else if (szDirection == "s")
				{
					//The adjacent room will be directly below the current room on the y-axis, so subtract the distance between rooms from the y offset
					fYOffset = -fRoomInterval;
					//To turn the path upside down, we rotate it 180 degrees around the z axis
					oConnection.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -180));
					//The position of the path is the mid-point between the current room and the adjacent room
					//So we subtract half the distance between rooms from the current room's y-axis position
					oConnection.transform.localPosition = oRoomCube.transform.localPosition + (new Vector3(0, -fRoomInterval / 2, 0));
				}
				//If the draw direction is "e" for east
				else if (szDirection == "e")
				{
					//The adjacent room will be on the left of the current room along the x axis, so add the distance between rooms to the x offset
					fXOffset = fRoomInterval;
					//To turn the path sideways, rotate it 90 degrees around the z axis
					oConnection.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -90));
					//The position of the path is the mid-point between the current room and the adjacent room
					//So we add half the distance between rooms to the current room's x-axis position
					oConnection.transform.localPosition = oRoomCube.transform.localPosition + (new Vector3(fRoomInterval / 2, 0, 0));
				}
				//If the draw direction is "w" for west
				else if (szDirection == "w")
				{
					//The adjacent room will be on the right of the current room along the x axis, so subtract the distance between rooms to the x offset
					fXOffset = -fRoomInterval;
					//To turn the path sideways, we rotate is 270 degrees around the z axis
					oConnection.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -270));
					//The position of the path is the mid-point between the current room and the adjacent room
					//So we subtract half the distance between rooms from the current room's x-axis position
					oConnection.transform.localPosition = oRoomCube.transform.localPosition + (new Vector3(-fRoomInterval / 2, 0, 0));
				}
				//If the draw direction is "u" for up
				else if (szDirection == "u")
				{
					//The adjacent room will be directly in front of the current room along the z axis, so subtract the distance between rooms from the z offset
					//(The lower the number along the z axis, the closer to the camera it is)
					fZOffset = -fRoomInterval;
					//To the path towards the camera, we rotate it 270 degrees around the x axis
					oConnection.transform.localRotation = Quaternion.Euler(new Vector3(270, 0, 0));
					//The position of the path is the mid-point between the current room and the adjacent room
					//So we subtract half the distance between rooms from the current room's z-axis position
					oConnection.transform.localPosition = oRoomCube.transform.localPosition + (new Vector3(0, 0, -fRoomInterval / 2));
				}
				//If the draw direction is "d" for down
				else if (szDirection == "d")
				{
					//The adjacent room will be directly behind the current room along the z axis, so add the distance between rooms from the z offset
					//(The higher than number along the z axis, the further away from the camera it is)
					fZOffset = fRoomInterval;
					//To turn the path away from the camera, we rotate it 90 degrees around the x axis
					oConnection.transform.localRotation = Quaternion.Euler(new Vector3(90, 0, 0));
					//The position of the path is the mid-point between the current room and the adjacent room
					//So we add half the distance between rooms to the current room's z-axis position
					oConnection.transform.localPosition = oRoomCube.transform.localPosition + (new Vector3(0, 0, fRoomInterval / 2));
				}
				//If the draw direction is "ne" for north-east
				else if (szDirection == "ne")
				{
					//The adjacent room will be above and to the right of the current room, so add the distance between rooms to the x and y offsets.
					fYOffset = fRoomInterval;
					fXOffset = fRoomInterval;
					//Because this is a diagonal connection, we use the hypotenuse of a triangle whose other two sides are the length of the distance between rooms
					//(We use four fifths of the hypotenuse so that there's a visible gap in the map if the room has not been visited.)
					oConnection.transform.localScale = new Vector3(0.25f, fHypotenuse * 0.8f, 0.25f);
					//To turn the path sideways, we rotate it 45 degrees around the z axis
					oConnection.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -45));
					//The position of the path is the mid-point between the current room and the adjacent room
					//So we add half the distance between rooms to the current room's x and y positions
					oConnection.transform.localPosition = oRoomCube.transform.localPosition + (new Vector3(fRoomInterval / 2, fRoomInterval / 2, 0));
				}
				else if (szDirection == "se")
				{
					//The adjacent room will be below and to the right of the current room, so subtract the distance between rooms from the y offset and add it to the x offset
					fYOffset = -fRoomInterval;
					fXOffset = fRoomInterval;
					//Because this is a diagonal connection, we use the hypotenuse of a triangle whose other two sides are the length of the distance between rooms
					//(We use four fifths of the hypotenuse so that there's a visible gap in the map if the room has not been visited.)
					oConnection.transform.localScale = new Vector3(0.25f, fHypotenuse * 0.8f, 0.25f);
					//To turn the path sideways, we rotate it 135 degrees around the z axis
					oConnection.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -135));
					//The position of the path is the mid-point between the current room and the adjacent room
					//So we add half the distance between rooms to the current room's x position and subtract it from its y position
					oConnection.transform.localPosition = oRoomCube.transform.localPosition + (new Vector3(fRoomInterval / 2, -fRoomInterval / 2, 0));
				}
				else if (szDirection == "sw")
				{
					//The adjacent room will be below and to the left of the current room, so subtract the distance between rooms to the x and y offsets
					fYOffset = -fRoomInterval;
					fXOffset = -fRoomInterval;
					//Because this is a diagonal connection, we use the hypotenuse of a triangle whose other two sides are the length of the distance between rooms
					//(We use four fifths of the hypotenuse so that there's a visible gap in the map if the room has not been visited.)
					oConnection.transform.localScale = new Vector3(0.25f, fHypotenuse * 0.8f, 0.25f);
					//To turn the path sideways, we rotate it 225 degrees around the z axis
					oConnection.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -225));
					//The position of the path is the mid-point between the current room and the adjacent room
					//So we subtract half the distance between rooms from the current room's x and y positions
					oConnection.transform.localPosition = oRoomCube.transform.localPosition + (new Vector3(-fRoomInterval / 2, -fRoomInterval / 2, 0));
				}
				else if (szDirection == "nw")
				{
					//The adjacent room will be above and to the left of the current room, so add the distance between rooms to the y offset, and subtract it from the x offset
					fYOffset = fRoomInterval;
					fXOffset = -fRoomInterval;
					//Because this is a diagonal connection, we use the hypotenuse of a triangle whose other two sides are the length of the distance between rooms
					//(We use four fifths of the hypotenuse so that there's a visible gap in the map if the room has not been visited.)
					oConnection.transform.localScale = new Vector3(0.25f, fHypotenuse * 0.8f, 0.25f);
					//To turn the path sideways, we rotate it 315 degrees around the z axis
					oConnection.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -315));
					//The position of the path is the mid-point between the current room and the adjacent room
					//So we subtract half the distance between rooms from the current room's x position and add it to its y position
					oConnection.transform.localPosition = oRoomCube.transform.localPosition + (new Vector3(-fRoomInterval / 2, fRoomInterval / 2, 0));
				}

			}

			//Set the next room to the room in the direction we're currently assessing (the adjacent room)
			oNextRoom = oCurrentRoom.GetRoomForDrawing(szDistanceAndDirection);

			//If that room hasn't already been drawn, and it has been flagged as visited
			if (!oCompleted.Contains(oNextRoom.Name) && oNextRoom.Visited)
				//Draw that room
				drawRooms (oCurrentRoom.GetRoomForDrawing(szDistanceAndDirection), fX + fXOffset, fY + fYOffset, fZ + fZOffset, oCompleted);
		}
	}

	//---------------------------------------------------------------------------------------------------------------
	//Description: Finds the highest and lowest x, y and z positions out of all the GameObjects that make up the map
	//             and uses those figures to instantiate the global maximum and minimum value vectors
	//---------------------------------------------------------------------------------------------------------------
	void SetMaxMinValues()
	{
		//An x-y-z vector containing the map's dimensions
		Vector3 oMapSize;

		//The minimum and maximum x values
		float fXMin = 0;
		float fXMax = 0;
		//The minimum and maximum y values
		float fYMin = 0;
		float fYMax = 0;
		//The minimum and maximum z values
		float fZMin = 0;
		float fZMax = 0;

		//Iterate through each child object (which will be rooms and passages between the rooms)
		foreach (Transform oChild in g_oAnchor.transform)
		{
			//If the x position of this object is less than the current value of the minimum x position
			if (oChild.transform.localPosition.x < fXMin)
				//Make this object's x position the minimum
				fXMin = oChild.localPosition.x;
			//If the x position of this object is greater than the current value of the maximum x position
			if (oChild.transform.localPosition.x > fXMax)
				//Make this object's x position the maximum
				fXMax = oChild.transform.localPosition.x;
			//If the y position of this object is less than the current value of the minimum y position
			if (oChild.transform.localPosition.y < fYMin)
				//Make this object's y position the minimum
				fYMin = oChild.transform.localPosition.y;
			//If the y position of this object is greater than the current value of the maximum y position
			if (oChild.transform.localPosition.y > fYMax)
				//Make this object's y position the maximum
				fYMax = oChild.transform.localPosition.y;
			//If the z position of this object is less than the current value of the minimum z position
			if (oChild.transform.localPosition.z < fZMin)
				//Make this object's z position the minimum
				fZMin = oChild.transform.localPosition.z;
			//If the z position of this object is greater than the current value of the maximum z position
			if (oChild.transform.localPosition.z > fZMax)
				//Make this object's z position the maximum
				fZMax = oChild.transform.localPosition.z;
		}

		//Set the maximum value vector using the maximum x, y, and z values
		MaxValues = new Vector3 (fXMax, fYMax, fZMax);
		//Set the minimum value vector using the minimum x, y and z values
		MinValues = new Vector3 (fXMin, fYMin, fZMin);

		/*
		fXMin -= 0.5f;
		fXMax += 0.5f;
		fYMin -= 0.5f;
		fYMax += 0.5f;
		fZMin -= 0.5f;
		fZMax += 0.5f;
		*/
	}


	//-------------------------------------------------------------------------------------
	//Description: Populates the map, creating the Rooms and then connecting them together
	//-------------------------------------------------------------------------------------
	void buildMap(string[] szAllRooms)
	{
		//The room to be created
		Room oNewRoom;
		//The name of the room to be created
		string szNewRoom;

		//This array contains the information of the room currently being populated with attached rooms
		//It contains the name of the room at index zero
		//At all the other index is a direction and the name of an attached room, seperated by a ':' character
		string[] szOneRoomAndAttachedRooms;

		//For each room listed in the All Rooms array argument
		foreach (string szRoomInfo in szAllRooms)
		{
			//The name of the new room to be created is at the start of the room info string, before the first '|'
			szNewRoom = szRoomInfo.Split('|')[0];
			//Instantiate the new room
			oNewRoom = new Room(szNewRoom);
			//Add the new room to the rooms dictionary, with its name as the key
			g_oRooms.Add(szNewRoom, oNewRoom);
		}

		//Iterate through each room in the rooms dictionary
		foreach(KeyValuePair<string, Room> oEntry in g_oRooms)
		{
			//For each room listed in the All Rooms array argument
			foreach (string szRoomInfo in szAllRooms)
			{
				//Create an array with the name of the room at index zero and the direction and name of the room to be attached in all the other indexes
				szOneRoomAndAttachedRooms = szRoomInfo.Split('|');

				//If the key to the current room (ie: the room name) is equal to the name of the room at index zero
				if (oEntry.Key == szOneRoomAndAttachedRooms[0])
				{
					//Iterate through the index of the array
					for (int iIndex = 1; iIndex < szOneRoomAndAttachedRooms.Length; iIndex++)
					{
						//Add the connected room to this room
						//The draw direction is listed before ethe first ':'
						//The true direction (the one used by Inform7) is listed after the first ':' and before the second ':'
						//The name of the room is listed after the second ':'
						oEntry.Value.SetConnectedRoom(szOneRoomAndAttachedRooms[iIndex].Split(':')[0], szOneRoomAndAttachedRooms[iIndex].Split(':')[1], g_oRooms[szOneRoomAndAttachedRooms[iIndex].Split(':')[2]]);
					}
				}
			}
		}
	}
}
