import copy, unittest

from balance import Game,Unit,rotate,series,hypergeom_no



def card(name='Thornwatch Stag',power=2,defense=3,arrows=0,guard=False):

 return dict(cardName=name,power=power,defense=defense,attackArrows=arrows,guard=guard,grade=1,genericEssenceCost=0,canPlayInFront=True,canPlayInRear=True,essence=1,rearProduces=1)

def deck():

 return dict(name='Fixture',main=[dict(quantity=20,card=card())],evolution=[dict(card=card('Yggdrasil, Living Citadel',5,7,84),evolvesFrom='Heartwood Herald')])

class RulesTest(unittest.TestCase):

 def setUp(self): self.g=Game(deck(),deck(),42)

 def unit(self,name,p=0,col=2,row=1,**kw):

  u=Unit(card(name,**kw),p,col,row,ready=True);self.g.units.append(u);return u

 def test_clockwise_rotation(self): self.assertEqual(rotate(1),4);self.assertEqual(rotate(68),17);self.assertEqual(rotate(rotate(rotate(rotate(131)))),131)

 def test_arrow_geometry_both_players(self):

  a=self.unit('Broodgate Demon',arrows=131);b=self.unit('Broodgate Demon',p=1,arrows=131)

  self.assertEqual(self.g.arrows(a),{(1,1,1),(1,1,2),(1,1,3)});self.assertEqual(self.g.arrows(b),{(0,1,1),(0,1,2),(0,1,3)})

 def test_arrow_edges_do_not_wrap(self):

  a=self.unit('Briar Warden',col=0,arrows=68);self.assertEqual(self.g.arrows(a),{(0,1,1)})

 def test_placement_changes_support(self):

  a=self.unit('Briar Warden',arrows=68);b=self.unit('Vine Scout',col=3)

  self.assertEqual(self.g.stats(b)[0],3); b.col=4;self.assertEqual(self.g.stats(b)[0],2)

 def test_guard_rotation_protects_rear(self):

  a=self.unit('Briar Warden',arrows=68);a.guard=True;b=self.unit('Vine Scout',row=0)

  self.assertEqual(self.g.stats(b)[1],4)

 def test_silk_only_affects_pointed_attacker(self):

  self.unit('Silkspinner',arrows=130);b=self.unit('Vine Scout',p=1,col=3)

  self.assertEqual(self.g.stats(b,True)[0],1);self.assertEqual(self.g.stats(b,False)[0],2)

 def test_simultaneous_death_no_scars(self):

  a=self.unit('Vine Scout',power=3,defense=2);b=self.unit('Shade Imp',p=1,power=3,defense=2)

  self.g.battle(a,b);self.assertEqual(len(self.g.units),0);self.assertEqual(a.scars,0)

 def test_survival_scars_once_per_battle(self):

  a=self.unit('Heartwood Herald',power=1,defense=5);b=self.unit('Shade Imp',p=1,power=1,defense=5)

  self.g.battle(a,b);self.assertEqual(a.scars,1);self.assertEqual(a.damage,1)

 def test_zero_damage_no_scar(self):

  a=self.unit('Vine Scout',power=0);b=self.unit('Shade Imp',p=1,power=0)

  self.g.battle(a,b);self.assertEqual(a.scars+b.scars,0)

 def test_adjacent_guard_intercepts(self):

  a=self.unit('Vine Scout');b=self.unit('Shade Imp',p=1);g=self.unit('Thornwatch Stag',p=1,col=3,guard=True);g.guard=True

  self.g.battle(a,b);self.assertEqual(b.damage,0);self.assertFalse(g.ready);self.assertEqual(self.g.metrics['intercepts'],1)

 def test_guard_intercepts_open_lane(self):

  a=self.unit('Vine Scout');g=self.unit('Thornwatch Stag',p=1,col=3,guard=True);g.guard=True

  self.g.battle(a,None);self.assertEqual(self.g.life[1],20);self.assertEqual(g.damage,2)

 def test_demon_debuff_only_during_attack(self):

  self.unit('Broodgate Demon',arrows=131);b=self.unit('Shade Imp',p=1)

  self.assertEqual(self.g.stats(b)[1],3);self.g.in_battle=True;self.assertEqual(self.g.stats(b)[1],2)

 def test_distant_guard_cannot_intercept(self):

  a=self.unit('Vine Scout');b=self.unit('Shade Imp',p=1);g=self.unit('Thornwatch Stag',p=1,col=4,guard=True);g.guard=True

  self.g.battle(a,b);self.assertEqual(b.damage,2)

 def test_web_consumed_and_skips_ready(self):

  u=self.unit('Vine Scout',col=1);self.g.web.add((0,2));self.g.enter(u,1,2,True)

  self.assertTrue(u.web_skip);self.assertFalse(u.ready);self.assertNotIn((0,2),self.g.web)

 def test_moth_buff_does_not_stack(self):

  u=self.unit('Grave Moth');self.g.left((1,1,2));self.g.left((1,1,2));self.assertEqual(u.next_power,1)

 def test_skitter_promotes_into_own_empty_front(self):

  u=self.unit('Hollow Skitter',row=0);self.g.left((1,1,2));self.assertEqual(u.row,1);self.assertFalse(u.ready)

 def test_queen_requires_arrowed_destination(self):

  q=self.unit('Queen of the Unclaimed Path',arrows=84);u=self.unit('Vine Scout',col=0)

  self.g.enter(u,1,1,True);self.assertEqual(u.next_power,1);self.g.enter(u,1,0,True);self.assertTrue(q.used)

 def test_citadel_save_uses_arrow_and_prevents_packet(self):

  q=self.unit('Yggdrasil, Living Citadel',arrows=84);u=self.unit('Vine Scout',col=3);u.damage=99

  self.assertTrue(self.g.rescue(u,1));self.assertEqual(u.row,0);self.assertEqual(u.damage,1);self.assertTrue(q.used)

 def test_triumph_requires_scars_and_contiguous_columns(self):

  u=self.unit('Heartwood Herald');u.scars=2;self.unit('Vine Scout',col=1);self.unit('Vine Scout',col=3,row=0)

  self.g.active=1;self.g.evolve(0,False);self.assertEqual(u.name,'Yggdrasil, Living Citadel');self.assertEqual(u.scars,2)

 def test_no_free_evolution_reset(self):

  u=self.unit('Heartwood Herald');u.scars=2;u.damage=3;u.ready=False;self.unit('Vine Scout',col=1);self.unit('Vine Scout',col=3,row=0)

  self.g.active=1;self.g.evolve(0,False);self.assertEqual(u.damage,2);self.assertFalse(u.ready)

 def test_spectre_cannot_promote_from_rear(self):
  s=self.unit('Ruin Spectre',row=0,col=3);u=self.unit('Vine Scout',col=0)
  self.g.enter(u,1,1,True);self.assertEqual(s.row,0)
 def test_evolution_copy_cannot_be_reused(self):
  u=self.unit('Heartwood Herald');u.scars=2;self.unit('Vine Scout',col=1);self.unit('Vine Scout',col=3,row=0)
  self.g.active=1;self.g.evolve(0,False);self.g.units.remove(u)
  v=self.unit('Heartwood Herald');v.scars=2;self.g.evolve(0,False);self.assertEqual(v.name,'Heartwood Herald')
 def test_reproducible_series(self): self.assertEqual(series(deck(),deck(),4,99),series(deck(),deck(),4,99))

 def test_mirror_pair_identity(self):

  r=series(deck(),deck(),5,99);self.assertEqual(r['wins'][0],r['wins'][1]);self.assertEqual(r['score'],.5)

 def test_opening_probability(self): self.assertAlmostEqual(hypergeom_no(20,11,5),126/15504)

if __name__=='__main__': unittest.main()

