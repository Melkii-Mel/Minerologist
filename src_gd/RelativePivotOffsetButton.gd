tool
extends Control

export(float, 0.0, 1.0) var relative_pivot := 0.5 setget set_relative_pivot

func set_relative_pivot(value: float) -> void:
	relative_pivot = value
	_update_pivot()

func _ready() -> void:
	_update_pivot()

func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		_update_pivot()

func _update_pivot() -> void:
	rect_pivot_offset = rect_size * relative_pivot
