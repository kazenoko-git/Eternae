extends CharacterBody3D

# Configure movement parameters
const SPEED = 5.0
const SPRINT_SPEED = 8.0
const JUMP_VELOCITY = 4.5
const ACCELERATION = 25.0
const FRICTION = 30.0
const ROTATION_SPEED = 10.0

# Mouse sensitivity
const MOUSE_SENSITIVITY = 0.003

# Get the gravity from the project settings to be synced with RigidBody nodes.
var gravity = ProjectSettings.get_setting("physics/3d/default_gravity")

# Node references
@onready var camera_pivot: SpringArm3D = $CameraPivot
@onready var visual_mesh: MeshInstance3D = $MeshInstance3D

func _ready() -> void:
	# Capture mouse cursor for camera control
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	
	# Helper to ensure inputs are mapped if they don't exist
	_setup_default_inputs()

func _unhandled_input(event: InputEvent) -> void:
	# Handle mouse look
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		camera_pivot.rotate_y(-event.relative.x * MOUSE_SENSITIVITY)
		camera_pivot.rotation.x = clamp(camera_pivot.rotation.x - event.relative.y * MOUSE_SENSITIVITY, deg_to_rad(-60), deg_to_rad(60))
	
	# Toggle mouse capture with ESC
	if event.is_action_pressed("ui_cancel"):
		if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		else:
			Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _physics_process(delta: float) -> void:
	# Add gravity.
	if not is_on_floor():
		velocity.y -= gravity * delta

	# Handle jump.
	if Input.is_action_just_pressed("jump") and is_on_floor():
		velocity.y = JUMP_VELOCITY
	
	# Sprint modifier
	var current_speed = SPEED
	if Input.is_action_pressed("sprint"):
		current_speed = SPRINT_SPEED

	# Get the input direction and handle the movement/deceleration.
	var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_backward")
	
	# Calculate direction relative to camera
	var direction := (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	direction = direction.rotated(Vector3.UP, camera_pivot.rotation.y).normalized()
	
	if direction:
		# Accelerate towards direction
		velocity.x = move_toward(velocity.x, direction.x * current_speed, ACCELERATION * delta)
		velocity.z = move_toward(velocity.z, direction.z * current_speed, ACCELERATION * delta)
		
		# Smoothly rotate character to face movement direction
		var target_rotation = atan2(direction.x, direction.z)
		visual_mesh.rotation.y = lerp_angle(visual_mesh.rotation.y, target_rotation, ROTATION_SPEED * delta)
	else:
		# Decelerate (Friction)
		velocity.x = move_toward(velocity.x, 0, FRICTION * delta)
		velocity.z = move_toward(velocity.z, 0, FRICTION * delta)

	move_and_slide()

# Utility to add default keybinds if missing (Standard WASD + Space + Shift)
func _setup_default_inputs():
	var inputs = {
		"move_forward": KEY_W,
		"move_backward": KEY_S,
		"move_left": KEY_A,
		"move_right": KEY_D,
		"jump": KEY_SPACE,
		"sprint": KEY_SHIFT
	}
	
	for action in inputs:
		if not InputMap.has_action(action):
			InputMap.add_action(action)
			var key = InputEventKey.new()
			key.keycode = inputs[action]
			InputMap.action_add_event(action, key)
