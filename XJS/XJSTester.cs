#if UNITY_2018 || UNITY_2019
#define UNITY
#endif
// Unity detection
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define COMP_SERVICES
using System.Runtime.CompilerServices;
using System.Diagnostics;

#endif
// Use UnityEngine's provided utilities.
using UnityEngine;


#else
// Hook into some other useful diagnostic stuff
#define COMP_SERVICES
using System.Runtime.CompilerServices;
using System.Diagnostics;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Concurrent;
using static JsonTests.TestFramework;

namespace JsonTests {

#if DEBUG
	public static class XJS_Tests {

		public static void TestX() {


		}

	}

#endif
}
