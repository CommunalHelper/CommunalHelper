
if you want to reskin those player animations, please add these line in player ID of Sprites.xml.
such as:
-------------------------------------------
    <player_no_backpack path="characters/player_no_backpack/" copy="player">
	  ......
	  <!-- (in case, these line) -->
	  <Anim id="anim_player_elytra_fly" path="CommunalHelper/fly" frames="0-8" delay="0"/>
	  
	  
	  ......
    </player_no_backpack>
-------------------------------------------