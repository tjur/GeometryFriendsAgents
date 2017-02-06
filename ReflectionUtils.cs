using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends;
using System.Drawing;

namespace GeometryFriendsAgents
{
    class ReflectionUtils
    {
        private static BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static FieldInfo _circleField;
        private static FieldInfo _bodyField;
        private static FieldInfo _bodyRotationField;
        private static FieldInfo _bodyAngularVelocityField;
        private static FieldInfo _bodyPreviousRotationField;
        private static FieldInfo _bodyPreviousAngularVelocityField;
        private static FieldInfo _bodyLinearVelocity;

        private static FieldInfo _vector2X;
        private static FieldInfo _vector2Y;

        public static object GetAssociatedCircleCharacter(ActionSimulator simulator)
        {
            if (_circleField == null) _circleField = simulator.GetType().GetField("<AssociatedCircleCharacter>k__BackingField", _bindingFlags);
            return _circleField.GetValue(simulator);
        }

        public static object GetBody(object associatedCircleCharacter)
        {
            if (_bodyField == null) _SetBodyFields(associatedCircleCharacter);
            return _bodyField.GetValue(associatedCircleCharacter);
        }

        public static PointF GetBodyLinearVelocity(object body)
        {
            var linearVelocity = _bodyLinearVelocity.GetValue(body);
            return new PointF((float)_vector2X.GetValue(linearVelocity), (float)_vector2Y.GetValue(linearVelocity));
        }

        public static void ListAllProperties(object o)
        {
            var fieldsValues = o.GetType().GetFields(_bindingFlags).ToList();

            foreach (var field in fieldsValues)
                Log.LogInformation(string.Format("{0} t: {1} v:{2}", field.Name, field.GetValue(o)?.GetType(), field.GetValue(o)));
        }

        public static void SetSimulator(ActionSimulator simulator, PointF position, PointF linearVelocity)
        {
            var circle = GetAssociatedCircleCharacter(simulator);
            var body = GetBody(circle);

            _SetXYOfVector2(body, "position", position);
            _SetXYOfVector2(body, "linearVelocity", linearVelocity);
            _SetXYOfVector2(body, "acceleration", new PointF(0, 0));
            _SetXYOfVector2(body, "previousPosition", position);
            _SetXYOfVector2(body, "previousLinearVelocity", linearVelocity);
            _bodyRotationField.SetValue(body, 0);
            _bodyAngularVelocityField.SetValue(body, 0);
            _bodyPreviousRotationField.SetValue(body, 0);
            _bodyPreviousAngularVelocityField.SetValue(body, 0);
        }

        private static void _SetBodyFields(object associatedCircleCharacter)
        {
            _bodyField = associatedCircleCharacter.GetType().GetField("body", _bindingFlags);
            var body = _bodyField.GetValue(associatedCircleCharacter);

            var bodyType = body.GetType();
            _bodyRotationField = bodyType.GetField("rotation", _bindingFlags);
            _bodyAngularVelocityField = bodyType.GetField("angularVelocity", _bindingFlags);
            _bodyPreviousRotationField = bodyType.GetField("previousRotation", _bindingFlags);
            _bodyPreviousAngularVelocityField = bodyType.GetField("previousAngularVelocity", _bindingFlags);
            _bodyLinearVelocity = bodyType.GetField("linearVelocity", _bindingFlags);

            var vector2Type = associatedCircleCharacter.GetType().GetField("position", _bindingFlags).GetValue(associatedCircleCharacter).GetType();
            _vector2X = vector2Type.GetField("X");
            _vector2Y = vector2Type.GetField("Y");
        }

        private static void _SetXYOfVector2(object o, string fieldName, PointF p)
        {
            FieldInfo vectorField = o.GetType().GetField(fieldName, _bindingFlags);
            var vector = vectorField.GetValue(o);

            _vector2X.SetValue(vector, p.X);
            _vector2Y.SetValue(vector, p.Y);
            vectorField.SetValue(o, vector);
        }
    }
}

/*
simulator.AssociatedCircleCharacter:

[2017.02.04-23:19:24]INFO: radius t: System.Int32 v:40
[2017.02.04-23:19:24]INFO: centroidDebug t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.04-23:19:24]INFO: verticesDebug t:  v:
[2017.02.04-23:19:24]INFO: mass t: System.Int32 v:3
[2017.02.04-23:19:24]INFO: isBig t: System.Boolean v:False
[2017.02.04-23:19:24]INFO: isSmall t: System.Boolean v:False
[2017.02.04-23:19:24]INFO: jumpingDelayInterval t: System.Double v:210
[2017.02.04-23:19:24]INFO: collisionState t: System.Boolean v:False
[2017.02.04-23:19:24]INFO: physicsSimulator t: FarseerGames.FarseerPhysics.PhysicsSimulator v:FarseerGames.FarseerPhysics.PhysicsSimulator
[2017.02.04-23:19:24]INFO: collisionCategory t: FarseerGames.FarseerPhysics.Enums+CollisionCategories v:All
[2017.02.04-23:19:24]INFO: collidesWith t: FarseerGames.FarseerPhysics.Enums+CollisionCategories v:All
[2017.02.04-23:19:24]INFO: detect_max_growth t: System.Boolean v:False
[2017.02.04-23:19:24]INFO: spin t: GeometryFriends.Levels.Shared.CircleCharacter+Spin v:None
[2017.02.04-23:19:24]INFO: <DebugDraw>k__BackingField t: System.Boolean v:False
[2017.02.04-23:19:24]INFO: <debugFont>k__BackingField t:  v:
[2017.02.04-23:19:24]INFO: position t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:1048 Y:88}
[2017.02.04-23:19:24]INFO: initialPosition t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:1048 Y:88}
[2017.02.04-23:19:24]INFO: body t: FarseerGames.FarseerPhysics.Dynamics.Body v:FarseerGames.FarseerPhysics.Dynamics.Body
[2017.02.04-23:19:24]INFO: geom t: FarseerGames.FarseerPhysics.Collisions.Geom v:FarseerGames.FarseerPhysics.Collisions.Geom
*/
/*
associatedCircleCharacter.body:

[2017.02.05-22:27:05]INFO: mass t: System.Single v:3
[2017.02.05-22:27:05]INFO: inverseMass t: System.Single v:0,3333333
[2017.02.05-22:27:05]INFO: momentOfInertia t: System.Single v:2400
[2017.02.05-22:27:05]INFO: inverseMomentOfInertia t: System.Single v:0,0004166667
[2017.02.05-22:27:05]INFO: position t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:1048 Y:93,3722}
[2017.02.05-22:27:05]INFO: rotation t: System.Single v:6,109334
[2017.02.05-22:27:05]INFO: revolutions t: System.Int32 v:-1
[2017.02.05-22:27:05]INFO: totalRotation t: System.Single v:-0,173852
[2017.02.05-22:27:05]INFO: linearVelocity t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:56,69823}
[2017.02.05-22:27:05]INFO: angularVelocity t: System.Single v:-1,942713
[2017.02.05-22:27:05]INFO: previousPosition t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:1048 Y:93,34385}
[2017.02.05-22:27:05]INFO: previousRotation t: System.Single v:6,110305
[2017.02.05-22:27:05]INFO: previousLinearVelocity t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:56,54824}
[2017.02.05-22:27:05]INFO: previousAngularVelocity t: System.Single v:-1,937505
[2017.02.05-22:27:05]INFO: linearVelocityBias t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: angularVelocityBias t: System.Single v:0
[2017.02.05-22:27:05]INFO: force t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: impulse t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: torque t: System.Single v:-50000
[2017.02.05-22:27:05]INFO: linearDragCoefficient t: System.Single v:0,001
[2017.02.05-22:27:05]INFO: rotationalDragCoefficient t: System.Single v:0,001
[2017.02.05-22:27:05]INFO: tag t:  v:
[2017.02.05-22:27:05]INFO: ignoreGravity t: System.Boolean v:False
[2017.02.05-22:27:05]INFO: isStatic t: System.Boolean v:False
[2017.02.05-22:27:05]INFO: enabled t: System.Boolean v:True
[2017.02.05-22:27:05]INFO: Updated t: FarseerGames.FarseerPhysics.Dynamics.Body+UpdatedEventHandler v:FarseerGames.FarseerPhysics.Dynamics.Body+UpdatedEventHandler
[2017.02.05-22:27:05]INFO: Disposed t: System.EventHandler`1[System.EventArgs] v:System.EventHandler`1[System.EventArgs]
[2017.02.05-22:27:05]INFO: id t: System.Int32 v:4418
[2017.02.05-22:27:05]INFO: translationMatrixTemp t: FarseerGames.FarseerPhysics.Mathematics.Matrix v:FarseerGames.FarseerPhysics.Mathematics.Matrix
[2017.02.05-22:27:05]INFO: rotationMatrixTemp t: FarseerGames.FarseerPhysics.Mathematics.Matrix v:FarseerGames.FarseerPhysics.Mathematics.Matrix
[2017.02.05-22:27:05]INFO: bodyMatrixTemp t: FarseerGames.FarseerPhysics.Mathematics.Matrix v:FarseerGames.FarseerPhysics.Mathematics.Matrix
[2017.02.05-22:27:05]INFO: worldPositionTemp t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: localPositionTemp t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: tempVelocity t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: r1 t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: velocityTemp t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: diff t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: speed t: System.Single v:0
[2017.02.05-22:27:05]INFO: rotationalDrag t: System.Single v:0
[2017.02.05-22:27:05]INFO: dragDirection t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: linearDrag t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: dv t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: acceleration t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: dw t: System.Single v:0
[2017.02.05-22:27:05]INFO: dp t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: rotationChange t: System.Single v:0
[2017.02.05-22:27:05]INFO: bodylinearVelocity t: FarseerGames.FarseerPhysics.Mathematics.Vector2 v:{X:0 Y:0}
[2017.02.05-22:27:05]INFO: bodyAngularVelocity t: System.Single v:0
[2017.02.05-22:27:05]INFO: isDisposed t: System.Boolean v:False

*/
