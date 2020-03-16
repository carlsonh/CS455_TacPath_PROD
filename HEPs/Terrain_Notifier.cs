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










		Transform[] childTrans = transform.GetComponentsInChildren<Transform>(false);
		int numChildren = childTrans.Length;
		// Starting at 1 to skip parent transform

		HEU_OutputAttribute colorAttr = attrStore.GetAttribute("Cd");
		HEU_OutputAttribute neighborAttr = attrStore.GetAttribute("neighbours");



		////Pull in attributes from the Houdini heightfield terrain. These are stored on the attrStore.
		HEU_OutputAttribute waterAttr = attrStore.GetAttribute("water");
		HEU_OutputAttribute debrisAttr = attrStore.GetAttribute("debris");
		HEU_OutputAttribute heightAttr = attrStore.GetAttribute("height");
		HEU_OutputAttribute sedimentAttr = attrStore.GetAttribute("sediment");
		HEU_OutputAttribute bedrockAttr = attrStore.GetAttribute("bedrock");
		HEU_OutputAttribute noiseAttr = attrStore.GetAttribute("noise");


		///Issue when editing where the asset needs to be recooked
		Debug.Log(colorAttr._floatValues.Length);

		


		//Loop through all generated nodes
		for (int i = 1; i < numChildren; ++i)
		{
			

			string instanceName = "Instance" + i;
			if (childTrans[i].name.EndsWith(instanceName))
			{

				// Now apply health as scale value
				Vector3 scale = childTrans[i].localScale;

				// Health index is -1 due to child indices off by 1 because of parent
				scale.y = heightAttr._floatValues[i - 1];
				childTrans[i].localScale = scale;

                //Colorize by point color from Houdini
                childTrans[i].GetComponent<Renderer>().material.SetColor("_Color", new Color(colorAttr._floatValues[(i*3)-3],colorAttr._floatValues[(i*3)-2],colorAttr._floatValues[(i*3)-1]));




				////HARD CODED, likely a limit by the variable vec limits


				///Generate node paths. These are made in houdini using the nearpoints fn
				Node childNode = childTrans[i].GetComponent<Node>();
				childNode.ConnectsTo = new Node[4];
				childNode.ConnectsTo[0] = childTrans[neighborAttr._intValues[(i*4)-4]+1].GetComponent<Node>();
				childNode.ConnectsTo[1] = childTrans[neighborAttr._intValues[(i*4)-3]+1].GetComponent<Node>();
				childNode.ConnectsTo[2] = childTrans[neighborAttr._intValues[(i*4)-2]+1].GetComponent<Node>();
				childNode.ConnectsTo[3] = childTrans[neighborAttr._intValues[(i*4)-1]+1].GetComponent<Node>();



				///Create node weight array and init
				float[] _inputValues = waterAttr._floatValues;


				//Fill array based on which weight type is selected in editor
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
				


                
			}
		}
	}
}
