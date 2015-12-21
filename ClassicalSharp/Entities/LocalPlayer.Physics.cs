﻿using System;
using System.Drawing;
using ClassicalSharp.Renderers;
using OpenTK;
using OpenTK.Input;

namespace ClassicalSharp {
	
	public partial class LocalPlayer : Player {
			
		bool useLiquidGravity = false; // used by BlockDefinitions.
		bool canLiquidJump = true;
		void UpdateVelocityState( float xMoving, float zMoving ) {
			if( noClip && xMoving == 0 && zMoving == 0 )
				Velocity = Vector3.Zero;
			
			if( flying || noClip ) {
				Velocity.Y = 0; // eliminate the effect of gravity
				if( flyingUp || jumping ) {
					Velocity.Y = speeding ? 0.24f : 0.12f;
				} else if( flyingDown ) {
					Velocity.Y = speeding ? -0.24f : -0.12f;
				}
			} else if( jumping && TouchesAnyRope() && Velocity.Y > 0.02f ) {
				Velocity.Y = 0.02f;
			}
			
			if( !jumping ) {
				canLiquidJump = false; return;
			}
			
			bool touchWater = TouchesAnyWater();
			bool touchLava = TouchesAnyLava();
			if( touchWater || touchLava ) {
				BoundingBox bounds = CollisionBounds;
				int feetY = Utils.Floor( bounds.Min.Y ), bodyY = feetY + 1;
				int headY = Utils.Floor( bounds.Max.Y );
				if( bodyY > headY ) bodyY = headY;
				
				bounds.Max.Y = bounds.Min.Y = feetY;
				bool liquidFeet = TouchesAny( bounds, StandardLiquid );
				bounds.Min.Y = Math.Min( bodyY, headY );
				bounds.Max.Y = Math.Max( bodyY, headY );
				bool liquidRest = TouchesAny( bounds, StandardLiquid );
				
				bool pastJumpPoint = liquidFeet && !liquidRest && (Position.Y % 1 >= 0.4);
				if( !pastJumpPoint ) {
					canLiquidJump = true;
					Velocity.Y += speeding ? 0.08f : 0.04f;
				} else if( pastJumpPoint ) {
					// either A) jump bob in water B) climb up solid on side
					if( canLiquidJump || (collideX || collideZ) )
						Velocity.Y += touchLava ? 0.20f : 0.10f;
					canLiquidJump = false;
				}
			} else if( useLiquidGravity ) {
				Velocity.Y += speeding ? 0.08f : 0.04f;
				canLiquidJump = false;
			} else if( TouchesAnyRope() ) {
				Velocity.Y += speeding ? 0.15f : 0.10f;
				canLiquidJump = false;
			} else if( onGround ) {
				Velocity.Y = speeding ? jumpVel * 2 : jumpVel;
				canLiquidJump = false;
			}
		}
		
		bool StandardLiquid( byte block ) {
			return info.CollideType[block] == BlockCollideType.SwimThrough;
		}
		
		static Vector3 waterDrag = new Vector3( 0.8f, 0.8f, 0.8f ),
		lavaDrag = new Vector3( 0.5f, 0.5f, 0.5f ),
		ropeDrag = new Vector3( 0.5f, 0.85f, 0.5f ),
		normalDrag = new Vector3( 0.91f, 0.98f, 0.91f ),
		airDrag = new Vector3( 0.6f, 1f, 0.6f );
		const float liquidGrav = 0.02f, ropeGrav = 0.034f, normalGrav = 0.08f;
		
		void PhysicsTick( float xMoving, float zMoving ) {
			if( noClip )
				onGround = false;
			float multiply = (flying || noClip) ?
				(speeding ? SpeedMultiplier * 9 : SpeedMultiplier * 1.5f) :
				(speeding ? SpeedMultiplier : 1);
			float modifier = LowestSpeedModifier();
			float horMul = multiply * modifier;
			float yMul = Math.Max( 1f, multiply / 5 ) * modifier;
			
			if( TouchesAnyWater() && !flying && !noClip ) {
				MoveNormal( xMoving, zMoving, 0.02f * horMul, waterDrag, liquidGrav, 1 );
			} else if( TouchesAnyLava() && !flying && !noClip ) {
				MoveNormal( xMoving, zMoving, 0.02f * horMul, lavaDrag, liquidGrav, 1 );
			} else if( TouchesAnyRope() && !flying && !noClip ) {
				MoveNormal( xMoving, zMoving, 0.02f * 1.7f, ropeDrag, ropeGrav, 1 );
			} else {
				float factor = !(flying || noClip) && onGround ? 0.1f : 0.02f;
				float gravity = useLiquidGravity ? liquidGrav : normalGrav;
				if( flying || noClip )
					MoveFlying( xMoving, zMoving, factor * horMul, normalDrag, gravity, yMul );
				else
					MoveNormal( xMoving, zMoving, factor * horMul, normalDrag, gravity, yMul );

				if( BlockUnderFeet == Block.Ice && !(flying || noClip) ) {
					// limit components to +-0.25f by rescaling vector to [-0.25, 0.25]
					if( Math.Abs( Velocity.X ) > 0.25f || Math.Abs( Velocity.Z ) > 0.25f ) {
						float scale = Math.Min(
							Math.Abs( 0.25f / Velocity.X ), Math.Abs( 0.25f / Velocity.Z ) );
						Velocity.X *= scale;
						Velocity.Z *= scale;
					}
				} else if( onGround || flying ) {
					Velocity *= airDrag; // air drag or ground friction
				}
			}
		}
		
		void AdjHeadingVelocity( float x, float z, float factor ) {
			float dist = (float)Math.Sqrt( x * x + z * z );
			if( dist < 0.00001f ) return;
			if( dist < 1 ) dist = 1;

			float multiply = factor / dist;
			Velocity += Utils.RotateY( x * multiply, 0, z * multiply, YawRadians );
		}
		
		void MoveFlying( float xMoving, float zMoving, float factor, Vector3 drag, float gravity, float yMul ) {
			AdjHeadingVelocity( zMoving, xMoving, factor );
			float yVel = (float)Math.Sqrt( Velocity.X * Velocity.X + Velocity.Z * Velocity.Z );
			// make vertical speed the same as vertical speed.
			if( (xMoving != 0 || zMoving != 0) && yVel > 0.001f ) {
				Velocity.Y = 0;		
				yMul = 1;
				if( flyingUp || jumping ) Velocity.Y += yVel;
				if( flyingDown ) Velocity.Y -= yVel;
			}
			Move( xMoving, zMoving, factor, drag, gravity, yMul );
		}
		
		void MoveNormal( float xMoving, float zMoving, float factor, Vector3 drag, float gravity, float yMul ) {
			AdjHeadingVelocity( zMoving, xMoving, factor );
			Move( xMoving, zMoving, factor, drag, gravity, yMul );
		}
		
		void Move( float xMoving, float zMoving, float factor, Vector3 drag, float gravity, float yMul ) {
			Velocity.Y *= yMul;
			if( !noClip )
				MoveAndWallSlide();
			Position += Velocity;
			
			Velocity.Y /= yMul;
			Velocity *= drag;
			Velocity.Y -= gravity;
		}
		
		/// <summary> Calculates the jump velocity required such that when a client presses
		/// the jump binding they will be able to jump up to the given height. </summary>
		internal void CalculateJumpVelocity( float jumpHeight ) {
			jumpVel = 0;
			if( jumpHeight >= 256 ) jumpVel = 10.0f;
			if( jumpHeight >= 512 ) jumpVel = 16.5f;
			if( jumpHeight >= 768 ) jumpVel = 22.5f;
			
			while( GetMaxHeight( jumpVel ) <= jumpHeight ) {
				jumpVel += 0.01f;
			}
		}
		
		static double GetMaxHeight( float u ) {
			// equation below comes from solving diff(x(t, u))= 0
			// We only work in discrete timesteps, so test both rounded up and down.
			double t = 49.49831645 * Math.Log( 0.247483075 * u + 0.9899323 );
			return Math.Max( YPosAt( (int)t, u ), YPosAt( (int)t + 1, u ) );
		}
		
		static double YPosAt( int t, float u ) {
			// v(t, u) = (4 + u) * (0.98^t) - 4, where u = initial velocity
			// x(t, u) = Σv(t, u) from 0 to t (since we work in discrete timesteps)
			// plugging into Wolfram Alpha gives 1 equation as
			// (0.98^t) * (-49u - 196) - 4t + 50u + 196
			double a = Math.Exp( -0.0202027 * t ); //~0.98^t
			return a * ( -49 * u - 196 ) - 4 * t + 50 * u + 196;
		}
		
		float LowestSpeedModifier() {
			BoundingBox bounds = CollisionBounds;
			useLiquidGravity = false;
			float baseModifier = LowestModifier( bounds, false );
			bounds.Min.Y -= 0.1f; // also check block standing on
			float solidModifier = LowestModifier( bounds, true );
			return Math.Max( baseModifier, solidModifier );
		}
		
		float LowestModifier( BoundingBox bounds, bool checkSolid ) {
			Vector3I bbMin = Vector3I.Floor( bounds.Min );
			Vector3I bbMax = Vector3I.Floor( bounds.Max );
			float modifier = float.PositiveInfinity;
			
			for( int y = bbMin.Y; y <= bbMax.Y; y++ )
				for( int z = bbMin.Z; z <= bbMax.Z; z++ )
					for( int x = bbMin.X; x <= bbMax.X; x++ )
			{
				byte block = game.Map.SafeGetBlock( x, y, z );
				if( block == 0 ) continue;
				BlockCollideType type = info.CollideType[block];
				if( type == BlockCollideType.Solid && !checkSolid )
					continue;
				
				modifier = Math.Min( modifier, info.SpeedMultiplier[block] );
				if( block >= BlockInfo.CpeBlocksCount && type == BlockCollideType.SwimThrough )
					useLiquidGravity = true;
			}
			return modifier == float.PositiveInfinity ? 1 : modifier;
		}
	}
}