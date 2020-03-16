using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoudiniEngineUnity;


//////In editor, pathfinder tries to take the lightest-colored path

[ExecuteInEditMode]
public class Terrain_Notifier : MonoBehaviour
{
	///Variables

	public bool bInvertedWeights = true;

	public enum weightType { none, water, debris, height, sediment, bedrock, noise};

	public weightType _chosenWeightType = weightType.water;



	///Functionss

	void Update()
	{
		if(!Application.isPlaying)
		{
			InstancerCallback(); ///This makes performance terrible
		}
	}

	
	///Called on Asset recook or in edit mode
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




		//Collect all nodes from HEngine
		Transform[] childTrans = transform.GetComponentsInChildren<Transform>(false);
		int numChildren = childTrans.Length;
		// Starting at 1 to skip parent transform




		////Pull in attributes from the Houdini heightfield terrain. These are stored on the attrStore

		HEU_OutputAttribute colorAttr = attrStore.GetAttribute("Cd");
		HEU_OutputAttribute neighborAttr = attrStore.GetAttribute("neighbours");

		//Attributes used for weight generation (height also used for scaling)
		HEU_OutputAttribute heightAttr = attrStore.GetAttribute("height");
		HEU_OutputAttribute waterAttr = attrStore.GetAttribute("water");
		HEU_OutputAttribute debrisAttr = attrStore.GetAttribute("debris");
		HEU_OutputAttribute sedimentAttr = attrStore.GetAttribute("sediment");
		HEU_OutputAttribute bedrockAttr = attrStore.GetAttribute("bedrock");
		HEU_OutputAttribute noiseAttr = attrStore.GetAttribute("noise");


		///Check for issue when editing where the asset needs to be recooked
		Debug.Log(colorAttr._floatValues.Length);
		


		//Loop through all generated nodes
		for (int i = 1; i < numChildren; ++i)
		{
			
			///Make sure the node exists
			string instanceName = "Instance" + i;
			if (childTrans[i].name.EndsWith(instanceName))
			{

				// Now apply health as scale value
				Vector3 scale = childTrans[i].localScale;

				// Health index is -1 due to child indices off by 1 because of parent
				scale.y = heightAttr._floatValues[i - 1];
				childTrans[i].localScale = scale;

                //Colorize by point color from Houdini //!!This generates a unique material per node
                childTrans[i].GetComponent<Renderer>().material.SetColor("_Color", new Color(colorAttr._floatValues[(i*3)-3],colorAttr._floatValues[(i*3)-2],colorAttr._floatValues[(i*3)-1]));




				////Due to vector dim limits, this cannot have more paths w/o adding more vec attribs

				///Generate node paths. These are made in Houdini using the nearpoints fn
				Node childNode = childTrans[i].GetComponent<Node>();
				childNode.ConnectsTo = new Node[4];
				childNode.ConnectsTo[0] = childTrans[neighborAttr._intValues[(i*4)-4]+1].GetComponent<Node>();
				childNode.ConnectsTo[1] = childTrans[neighborAttr._intValues[(i*4)-3]+1].GetComponent<Node>();
				childNode.ConnectsTo[2] = childTrans[neighborAttr._intValues[(i*4)-2]+1].GetComponent<Node>();
				childNode.ConnectsTo[3] = childTrans[neighborAttr._intValues[(i*4)-1]+1].GetComponent<Node>();



				///Create node weight array and init
				float[] _inputValues = new float[waterAttr._floatValues.Length];


				//Fill array based on which weight type is selected in editor
				switch (_chosenWeightType)
				{
					case weightType.none:
						_inputValues.Init(1f); ///This was changed outside of unity and is untested, line 116 init also used to be in here
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



				///0-1 the weight (has issues with noises, somehow generating negative numbers)
				float _normalizedValue = (_inputValues[i-1] - Mathf.Min(_inputValues)) / Mathf.Max(_inputValues); //0-1'd


				//Set the node's weight, checking if editor wants weights inverted
				childNode.ownWeight = bInvertedWeights ?  1f - _normalizedValue : _normalizedValue;
                
			}
		}
	}
}
