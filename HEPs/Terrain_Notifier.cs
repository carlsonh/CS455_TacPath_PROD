using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoudiniEngineUnity;

[ExecuteInEditMode]
public class Terrain_Notifier : MonoBehaviour
{

	public bool bInvertedWeights = true;

	public enum weightType { none, water, debris, height, sediment, bedrock, noise};

	public weightType _chosenWeightType = weightType.water;





	void Update()
	{
		if(!Application.isPlaying)
		{
			InstancerCallback(); ///This makes performance terrible
		}
	}

	

    void InstancerCallback()
	{


		// Acquire the attribute storage component (HEU_OutputAttributesStore).
		// HEU_OutputAttributesStore contains a dictionary of attribute names to attribute data (HEU_OutputAttribute).
		// HEU_OutputAttributesStore is added to the generated gameobject when an attribute with name 
		// "hengine_attr_store" is created at the detail level.
		HEU_OutputAttributesStore attrStore = gameObject.GetComponent<HEU_OutputAttributesStore>();
		if (attrStore == null)
		{
			Debug.LogWarning("No HEU_OutputAttributesStore component found!");
			return;
		}
		Debug.Log("HEU attrib store found");

		// // Query for the health attribute (HEU_OutputAttribute).
		// // HEU_OutputAttribute contains the attribute info such as name, class, storage, and array of data.
		// // Use the name to get HEU_OutputAttribute.
		// // Can use HEU_OutputAttribute._type to figure out what the actual data type is.
		// // Note that data is stored in array. The size of the array corresponds to the data type.
		// // For instances, the size of the array is the point cound.
		// if (heightAttr != null)
		// {
		// 	//Debug.LogFormat("Found water attribute with data for {0} instances.", heightAttr._floatValues.Length);

		// 	for(int i = 0; i < heightAttr._floatValues.Length; ++i)
		// 	{
		// 		//Debug.LogFormat("{0} = {1}", i, heightAttr._floatValues[i]);
		// 	}
		// }

		// Example of how to map the attribute array values to instances
		// Get the generated instances as children of this gameobject.
		// Note that this will include the current parent as first element (so its number of children + 1 size)
		Transform[] childTrans = transform.GetComponentsInChildren<Transform>(false);
		int numChildren = childTrans.Length;
		// Starting at 1 to skip parent transform

		HEU_OutputAttribute colorAttr = attrStore.GetAttribute("Cd");
		HEU_OutputAttribute neighborAttr = attrStore.GetAttribute("neighbours");


		HEU_OutputAttribute waterAttr = attrStore.GetAttribute("water");
		HEU_OutputAttribute debrisAttr = attrStore.GetAttribute("debris");
		HEU_OutputAttribute heightAttr = attrStore.GetAttribute("height");
		HEU_OutputAttribute sedimentAttr = attrStore.GetAttribute("sediment");
		HEU_OutputAttribute bedrockAttr = attrStore.GetAttribute("bedrock");
		HEU_OutputAttribute noiseAttr = attrStore.GetAttribute("noise");


		Debug.Log(colorAttr._floatValues.Length);

		
        //Debug.Log(colorAttr._floatValues.Length);
        //Debug.Log(neighborAttr._intValues.Length);
        //Debug.Log(neighborAttr._intValues[0]);

		for (int i = 1; i < numChildren; ++i)
		{
			//Debug.LogFormat("Instance {0}: name = {1}", i, childTrans[i].name);

			// Can use the name to match up indices
			string instanceName = "Instance" + i;
			if (childTrans[i].name.EndsWith(instanceName))
			{
				// Now apply health as scale value
				Vector3 scale = childTrans[i].localScale;

				// Health index is -1 due to child indices off by 1 because of parent
				scale.y = heightAttr._floatValues[i - 1];
				childTrans[i].localScale = scale;

                //Colorize by point color
                childTrans[i].GetComponent<Renderer>().material.SetColor("_Color", new Color(colorAttr._floatValues[(i*3)-3],colorAttr._floatValues[(i*3)-2],colorAttr._floatValues[(i*3)-1]));




				////HARD CODED, likely a limit by the variable vec limits
				Node childNode = childTrans[i].GetComponent<Node>();
				childNode.ConnectsTo = new Node[4];
				childNode.ConnectsTo[0] = childTrans[neighborAttr._intValues[(i*4)-4]+1].GetComponent<Node>();
				childNode.ConnectsTo[1] = childTrans[neighborAttr._intValues[(i*4)-3]+1].GetComponent<Node>();
				childNode.ConnectsTo[2] = childTrans[neighborAttr._intValues[(i*4)-2]+1].GetComponent<Node>();
				childNode.ConnectsTo[3] = childTrans[neighborAttr._intValues[(i*4)-1]+1].GetComponent<Node>();




				float[] _inputValues = waterAttr._floatValues;

				switch (_chosenWeightType)
				{
					case weightType.none:
						_inputValues = new float[waterAttr._floatValues.Length];
						_inputValues.Init(1f);
						break;

					case weightType.water:
						_inputValues = waterAttr._floatValues;
						break;

					case weightType.debris:
						_inputValues = debrisAttr._floatValues;
						break;

					case weightType.height:
						_inputValues = heightAttr._floatValues;
						break;

					case weightType.sediment:
						_inputValues = sedimentAttr._floatValues;
						break;
					
					case weightType.bedrock:
						_inputValues = bedrockAttr._floatValues;
						break;
					case weightType.noise:
						_inputValues = noiseAttr._floatValues;
						break;

				}


				
				float _normalizedValue = (_inputValues[i-1] - Mathf.Min(_inputValues)) / Mathf.Max(_inputValues); //0-1'd
				childNode.ownWeight = bInvertedWeights ?  1f - _normalizedValue : _normalizedValue;
				


                //Pull neighbours
                //childTrans[i].GetComponent<Node>().ConnectsTo = new GameObject[];
			}
		}
	}
}
