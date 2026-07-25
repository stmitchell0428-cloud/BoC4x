using UnityEngine;

/// <summary>Eden-to-synod chronicle shown while the match world loads.</summary>
public static class LoadingNarrative
{
    public readonly struct Beat
    {
        public readonly string Chapter;
        public readonly string Body;

        public Beat(string chapter, string body)
        {
            Chapter = chapter;
            Body = body;
        }
    }

    static readonly Beat[] Beats =
    {
        new(
            "I. The Expulsion",
            "In the beginning God planted a garden in Eden and set Adam there to work it and keep it. " +
            "When he ate the forbidden fruit, the Lord drove him out  -  <i>eastward</i>  -  and placed cherubim " +
            "with a flaming sword to guard the way to the tree of life.\n\n" +
            "You do not begin in Paradise. Every match starts in exile, with the garden behind you and a " +
            "wild world before you."),
        new(
            "II. East of Eden",
            "East of Eden the ground was cursed. Thorns and thistles would rise; bread would come only " +
            "by painful toil. Yet humanity did not vanish  -  it spread, multiplied, and learned to shape " +
            "the land.\n\n" +
            "The hex map now forming is that fallen earth: not a second garden, but a field where Law " +
            "and Gospel must be preached anew."),
        new(
            "III. Cities of Rebellion",
            "Cain went out from the Lord's presence and built a city. Later generations raised Babel, " +
            "trusting towers rather than confession. Cities in this game are seats of population, culture, " +
            "and power  -  but also of pride, heresy, and schism if adherence falters.\n\n" +
            "Your synod will found <b>Wittenberg</b> only after wandering: preach, survey, and bind the catechism first."),
        new(
            "IV. Bread by Sweat",
            "God told Adam: <i>By the sweat of your face you shall eat bread.</i> Pastures yield food; " +
            "forests and hills yield timber and stone. Each turn, cities and districts must eat or starve.\n\n" +
            "Surplus draws settlers; famine drives them away. The growth phase at end turn is the old curse " +
            "made visible  -  the land gives, or it withholds."),
        new(
            "V. The World Takes Shape",
            "After the flood, nations spread across the earth. Wilderness, shore, and river hexes divide " +
            "the map into workable tiles  -  each with yields your cities may someday claim through culture " +
            "and borders.\n\n" +
            "Scouts range ahead of the settler, revealing fog and rating where a capital might thrive."),
        new(
            "VI. A Creation Still Good",
            "The fall did not unmake God's creation. Gold still glints in hills; fish still swim in coastal " +
            "waters; wheat and cattle still graze. Map resources stack on terrain to reward wise founding.\n\n" +
            "The Lutheran synod does not flee the world  -  it enters it, preaching Christ where Adam once hid."),
        new(
            "VII. Rivers That Remember",
            "Scripture says a river flowed out of Eden to water the garden, then parted into four heads. " +
            "The rivers and seas on this map are not Paradise restored  -  they are paths, barriers, and " +
            "future naval frontiers.\n\n" +
            "Coasts and streams mark where trade, tribute, and trial will meet the church's mission."),
        new(
            "VIII. Gifts Hidden in the Soil",
            "Stone for walls, timber for chapels, iron for the day swords are drawn  -  the earth yields " +
            "what cities need to build and train. Manuscripts represent the written confession: catechisms " +
            "copied, hymns sung, treatises studied.\n\n" +
            "Production each turn advances building queues and spreads culture through the ring around each city."),
        new(
            "IX. A Land to Be Shown",
            "The Lord called Abram to leave country and kindred for a land He would show him. Your match " +
            "begins likewise: a <b>settler</b> and <b>scout</b> placed in the wilderness, no capital yet, " +
            "no enemy synod in sight.\n\n" +
            "Homelands are chosen now  -  the hexes where your people will first set foot east of Eden."),
        new(
            "X. The Synod Sent",
            "Missionaries carry the gospel to unreached tiles. Pastors anchor parishes; bishops oversee cities; " +
            "an archbishop may rise when the synod spans many towns. Deaconesses and cantors strengthen comfort " +
            "where Law and Gospel must stay in balance.\n\n" +
            "Walther meters drift each turn. Crises of legalism or antinomianism can split the church if you " +
            "ignore confession  -  schismatic blocs break away when adherence collapses."),
        new(
            "XI. Go Forth",
            "The cherubim still guard the garden you cannot re-enter by works or towers. Christ is the door; " +
            "the Book of Concord is your confessional compass.\n\n" +
            "<b>Preach.</b> <b>Survey.</b> <b>Bind the catechism.</b> Press <b>F</b> to found Wittenberg. " +
            "Grow districts, research confession, and pursue fame  -  or watch the synod fracture into heresy.\n\n" +
            "The exile ends only in proclamation, not in reclaiming Eden by the sword."),
    };

    public static int BeatCount => Beats.Length;

    public static Beat GetBeat(int index) => Beats[Mathf.Clamp(index, 0, Beats.Length - 1)];
}
