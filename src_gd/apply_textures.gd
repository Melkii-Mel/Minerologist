tool
extends Spatial

export(Texture) var albedo_texture setget set_albedo_texture
export(Texture) var orm_texture setget set_orm_texture

var _material := SpatialMaterial.new()


func set_albedo_texture(value):
	albedo_texture = value
	_update_material()


func set_orm_texture(value):
	orm_texture = value
	_update_material()


func _update_material():
	_material.albedo_texture = albedo_texture

	if orm_texture != null:
		_material.ao_texture = orm_texture
		_material.ao_texture_channel = SpatialMaterial.TEXTURE_CHANNEL_RED

		_material.roughness_texture = orm_texture
		_material.roughness_texture_channel = SpatialMaterial.TEXTURE_CHANNEL_GREEN

		_material.metallic_texture = orm_texture
		_material.metallic_texture_channel = SpatialMaterial.TEXTURE_CHANNEL_BLUE

	_apply_material(self)


func _apply_material(node: Node):
	if node is MeshInstance:
		node.material_override = _material

	for child in node.get_children():
		_apply_material(child)


func _ready():
	_update_material()
